using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PurrVet.DTOs.Appointments;
using PurrVet.DTOs.Common;
using PurrVet.Infrastructure;
using PurrVet.Models;

namespace PurrVet.Controllers.Api.V1 {
    [ApiController]
    [Route("api/v1/appointments")]
    [Authorize(Policy = "OwnerOnly")]
    [Tags("Appointments")]
    public class AppointmentsController : ControllerBase {
        private readonly ApplicationDbContext _context;

        public AppointmentsController(ApplicationDbContext context) {
            _context = context;
        }

        [HttpGet]
        [EndpointSummary("List all appointments")]
        [EndpointDescription("Returns all appointments across the clinic. The owner's own appointments include full details; other owners' appointments show limited info.")]
        [ProducesResponseType(typeof(ApiResponse<List<AppointmentListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        public IActionResult GetAppointments() {
            var ownerId = User.GetOwnerId();

            var allAppointments = _context.Appointments
                .Include(a => a.Pet).ThenInclude(p => p.Owner)
                .Include(a => a.ServiceCategory)
                .Include(a => a.ServiceSubtype)
                .ToList();

            var appointments = allAppointments.Select(a => new AppointmentListItemDto {
                AppointmentId = a.AppointmentID,
                AppointmentDate = a.AppointmentDate,
                Status = a.Pet.OwnerID == ownerId ? a.Status : "Not Available",
                GroupId = a.GroupID,
                Notes = a.Pet.OwnerID == ownerId ? a.Notes : "Booked by another owner",
                PetId = a.PetID,
                PetName = a.Pet.Name,
                ServiceType = a.ServiceCategory?.ServiceType,
                ServiceSubtype = a.ServiceSubtype?.ServiceSubType,
                IsOwnAppointment = a.Pet.OwnerID == ownerId
            }).ToList();

            return Ok(new ApiResponse<List<AppointmentListItemDto>> { Success = true, Data = appointments });
        }

        [HttpPost]
        [EndpointSummary("Create an appointment")]
        [EndpointDescription("Book a single appointment for a pet. The time slot must not already be taken.")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
        public IActionResult CreateAppointment([FromBody] CreateAppointmentRequest request) {
            var ownerId = User.GetOwnerId();
            var userName = User.GetUserName();

            if (request.PetId == 0 || request.CategoryId == 0 || request.AppointmentDate == default)
                return BadRequest(new ApiErrorResponse { Message = "All required fields must be filled." });

            var pet = _context.Pets.FirstOrDefault(p => p.PetID == request.PetId && p.OwnerID == ownerId);
            if (pet == null)
                return NotFound(new ApiErrorResponse { Message = "Pet not found or does not belong to you." });

            var appointmentDate = request.AppointmentDate.Date;
            if (TimeSpan.TryParse(request.AppointmentTime, out var parsedTime))
                appointmentDate = appointmentDate.Add(parsedTime);

            bool taken = _context.Appointments.Any(a => a.AppointmentDate == appointmentDate);
            if (taken)
                return Conflict(new ApiErrorResponse { Message = "This time slot is already taken." });

            var appointment = new Appointment {
                PetID = request.PetId,
                CategoryID = request.CategoryId,
                SubtypeID = request.SubtypeId,
                AppointmentDate = appointmentDate,
                Notes = request.Notes ?? "No notes.",
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.Appointments.Add(appointment);
            _context.Notifications.Add(new Notification {
                Message = $"New appointment requested by Owner #{ownerId} for Pet ID: {request.PetId} on {appointmentDate:MMM dd, yyyy hh:mm tt}.",
                Type = "Appointment",
                TargetRole = "Staff"
            });

            _context.SaveChanges();

            return StatusCode(201, new ApiResponse {
                Success = true,
                Message = "Appointment added successfully!"
            });
        }

        [HttpPost("bulk")]
        [EndpointSummary("Create grouped appointments")]
        [EndpointDescription("Book multiple services at the same time slot as a grouped appointment. All services share the same date/time from the first valid item.")]
        [ProducesResponseType(typeof(ApiResponse<BulkAppointmentResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public IActionResult CreateBulkAppointment([FromBody] CreateBulkAppointmentRequest request) {
            var ownerId = User.GetOwnerId();
            var userName = User.GetUserName();

            if (request.Appointments == null || request.Appointments.Count == 0)
                return BadRequest(new ApiErrorResponse { Message = "No appointments provided." });

            var firstValid = request.Appointments.FirstOrDefault(a =>
                a.PetId != 0 && a.CategoryId != 0 &&
                a.AppointmentDate != default && !string.IsNullOrEmpty(a.AppointmentTime));

            if (firstValid == null)
                return BadRequest(new ApiErrorResponse { Message = "All services must have completed fields." });

            if (!TimeSpan.TryParse(firstValid.AppointmentTime, out var parsedTime))
                return BadRequest(new ApiErrorResponse { Message = "Invalid time format." });

            var groupDateTime = firstValid.AppointmentDate.Date.Add(parsedTime);

            var group = new AppointmentGroup {
                GroupTime = groupDateTime,
                Notes = "Grouped appointment (Owner - Mobile)",
                CreatedAt = DateTime.Now
            };

            _context.AppointmentGroups.Add(group);
            _context.SaveChanges();

            var added = new List<Appointment>();

            foreach (var a in request.Appointments) {
                if (a.PetId == 0 || a.CategoryId == 0) continue;

                var pet = _context.Pets.FirstOrDefault(p => p.PetID == a.PetId && p.OwnerID == ownerId);
                if (pet == null) continue;

                var appointment = new Appointment {
                    PetID = a.PetId,
                    CategoryID = a.CategoryId,
                    SubtypeID = a.SubtypeId,
                    AppointmentDate = groupDateTime,
                    Notes = a.Notes ?? "No notes.",
                    Status = "R",
                    GroupID = group.GroupID,
                    CreatedAt = DateTime.Now
                };

                _context.Appointments.Add(appointment);
                added.Add(appointment);
            }

            if (!added.Any())
                return BadRequest(new ApiErrorResponse { Message = "No valid appointments to add." });

            _context.SaveChanges();

            var dateText = groupDateTime.ToString("MMM dd, yyyy hh:mm tt");

            _context.Notifications.Add(new Notification {
                Message = $"New group appointment (#{group.GroupID}) requested by {userName} for {dateText} ({added.Count} services).",
                Type = "Appointment",
                TargetRole = "Staff"
            });

            foreach (var appt in added) {
                _context.Notifications.Add(new Notification {
                    Message = $"Appointment requested by {userName} for Pet ID: {appt.PetID} on {dateText}.",
                    Type = "Appointment",
                    TargetRole = "Staff"
                });
            }

            _context.SystemLogs.Add(new SystemLog {
                ActionType = "Bulk Create",
                Module = "Appointment",
                Description = $"Owner #{ownerId} created {added.Count} services in Group #{group.GroupID} scheduled for {dateText} (via mobile).",
                PerformedBy = userName,
                Timestamp = DateTime.Now
            });

            _context.SaveChanges();

            return StatusCode(201, new ApiResponse<BulkAppointmentResponse> {
                Success = true,
                Message = $"Successfully added {added.Count} service(s) (Group #{group.GroupID}).",
                Data = new BulkAppointmentResponse {
                    GroupId = group.GroupID,
                    Count = added.Count
                }
            });
        }

        [HttpPost("{id}/cancel")]
        [EndpointSummary("Request cancellation")]
        [EndpointDescription("Request cancellation for all appointments in a group. Only groups where every appointment is still pending/requested can be cancelled.")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public IActionResult RequestCancellation(int id) {
            var ownerId = User.GetOwnerId();
            var userName = User.GetUserName();

            var appointment = _context.Appointments
                .Include(a => a.Pet)
                .FirstOrDefault(a => a.AppointmentID == id);

            if (appointment == null)
                return NotFound(new ApiErrorResponse { Message = "Appointment not found." });

            if (appointment.Pet.OwnerID != ownerId)
                return StatusCode(403, new ApiErrorResponse { Message = "You do not have access to this appointment." });

            if (appointment.GroupID == null)
                return BadRequest(new ApiErrorResponse { Message = "This appointment is not part of a group." });

            var groupAppointments = _context.Appointments
                .Where(a => a.GroupID == appointment.GroupID)
                .ToList();

            var invalidStatuses = groupAppointments
                .Where(a => a.Status.ToLower() != "pending" && a.Status.ToLower() != "requested" && a.Status.ToLower() != "r")
                .ToList();

            if (invalidStatuses.Any())
                return BadRequest(new ApiErrorResponse { Message = "This group contains appointments that cannot be cancelled." });

            foreach (var appt in groupAppointments) {
                appt.Status = "Cancellation Requested";
                _context.Appointments.Update(appt);
            }

            _context.SystemLogs.Add(new SystemLog {
                ActionType = "Update",
                Module = "Appointment",
                Description = $"Owner requested cancellation for group #{appointment.GroupID} ({groupAppointments.Count} appointments) (via mobile).",
                PerformedBy = userName,
                Timestamp = DateTime.Now
            });

            _context.SaveChanges();

            return Ok(new ApiResponse {
                Success = true,
                Message = $"Cancellation request sent for Group #{appointment.GroupID} ({groupAppointments.Count} appointments)."
            });
        }

        [HttpGet("time-slots")]
        [EndpointSummary("Get available time slots")]
        [EndpointDescription("Returns all 5-minute time slots between 09:00 and 18:00 for a given date, indicating which are available or already booked.")]
        [ProducesResponseType(typeof(ApiResponse<TimeSlotsResponse>), StatusCodes.Status200OK)]
        public IActionResult GetAvailableTimeSlots([FromQuery] DateTime date) {
            var start = new TimeSpan(9, 0, 0);
            var end = new TimeSpan(18, 0, 0);
            var interval = TimeSpan.FromMinutes(5);
            var slots = new List<TimeSlotDto>();

            var now = DateTime.Now;
            bool isToday = date.Date == now.Date;

            var taken = _context.Appointments
                .Where(a => a.AppointmentDate.Date == date.Date && a.Status.ToLower() != "cancelled")
                .Select(a => new TimeSpan(a.AppointmentDate.Hour, a.AppointmentDate.Minute, 0))
                .ToList();

            for (var t = start; t <= end; t = t.Add(interval)) {
                bool isTaken = taken.Any(x => Math.Abs((x - t).TotalMinutes) < 1);

                if (isToday && DateTime.Today.Add(t) < now)
                    isTaken = true;

                slots.Add(new TimeSlotDto {
                    Time = DateTime.Today.Add(t).ToString("HH:mm"),
                    Available = !isTaken
                });
            }

            return Ok(new ApiResponse<TimeSlotsResponse> {
                Success = true,
                Data = new TimeSlotsResponse {
                    Date = date.ToString("yyyy-MM-dd"),
                    Slots = slots
                }
            });
        }

        [HttpGet("services")]
        [EndpointSummary("List service categories")]
        [EndpointDescription("Returns all available service categories with their subtypes, used when creating appointments.")]
        [ProducesResponseType(typeof(ApiResponse<List<ServiceCategoryDto>>), StatusCodes.Status200OK)]
        public IActionResult GetServices() {
            var categories = _context.ServiceCategories
                .Include(c => c.Subtypes)
                .Select(c => new ServiceCategoryDto {
                    CategoryId = c.CategoryID,
                    ServiceType = c.ServiceType,
                    Subtypes = c.Subtypes != null ? c.Subtypes.Select(s => new ServiceSubtypeDto {
                        SubtypeId = s.SubtypeID,
                        ServiceSubType = s.ServiceSubType
                    }).ToList() : new List<ServiceSubtypeDto>()
                }).ToList();

            return Ok(new ApiResponse<List<ServiceCategoryDto>> { Success = true, Data = categories });
        }
    }
}
