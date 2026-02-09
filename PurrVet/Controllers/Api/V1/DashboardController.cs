using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetCloud.DTOs.Common;
using PetCloud.DTOs.Dashboard;
using PetCloud.Infrastructure;
using PetCloud.Models;

namespace PetCloud.Controllers.Api.V1 {
    [ApiController]
    [Route("api/v1/dashboard")]
    [Authorize(Policy = "OwnerOnly")]
    [Tags("Dashboard")]
    public class DashboardController : ControllerBase {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context) {
            _context = context;
        }

        [HttpGet]
        [EndpointSummary("Get owner dashboard")]
        [EndpointDescription("Returns the owner's dashboard data including their pets, upcoming appointments, and vaccination/deworming items due within 5 days.")]
        [ProducesResponseType(typeof(ApiResponse<DashboardResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        public IActionResult GetDashboard() {
            var ownerId = User.GetOwnerId();
            var userName = User.GetUserName();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var pets = _context.Pets
                .Where(p => p.OwnerID == ownerId)
                .OrderByDescending(p => p.PetID)
                .Select(p => new DashboardPetDto {
                    PetId = p.PetID,
                    Name = p.Name,
                    Breed = p.Breed,
                    PhotoUrl = p.PhotoPath != null ? baseUrl + p.PhotoPath : null,
                    Birthdate = p.Birthdate
                }).ToList();

            var upcomingAppointments = _context.Appointments
                .Include(a => a.Pet)
                .Include(a => a.ServiceCategory)
                .Where(a => a.Pet.OwnerID == ownerId &&
                            (a.Status == "Pending" || a.AppointmentDate >= DateTime.Now))
                .OrderBy(a => a.AppointmentDate)
                .Select(a => new DashboardAppointmentDto {
                    AppointmentId = a.AppointmentID,
                    AppointmentDate = a.AppointmentDate,
                    Status = a.Status,
                    PetId = a.Pet.PetID,
                    PetName = a.Pet.Name,
                    ServiceType = a.ServiceCategory != null ? a.ServiceCategory.ServiceType : null
                }).ToList();

            var vaccineDue = _context.Appointments
                .Include(a => a.Pet)
                .Include(a => a.ServiceCategory)
                .Where(a => a.Pet.OwnerID == ownerId &&
                            a.DueDate != null &&
                            a.DueDate <= DateTime.Now.AddDays(5) &&
                            a.ServiceCategory.ServiceType.Contains("Vaccination"))
                .OrderBy(a => a.DueDate)
                .Select(a => new DashboardVaccineDueDto {
                    AppointmentId = a.AppointmentID,
                    DueDate = a.DueDate,
                    PetId = a.Pet.PetID,
                    PetName = a.Pet.Name,
                    ServiceType = a.ServiceCategory.ServiceType
                }).ToList();

            var dewormDue = _context.Appointments
                .Include(a => a.Pet)
                .Include(a => a.ServiceCategory)
                .Where(a => a.Pet.OwnerID == ownerId &&
                            a.DueDate != null &&
                            a.DueDate <= DateTime.Now.AddDays(5) &&
                            a.ServiceCategory.ServiceType.Contains("Deworming & Preventives"))
                .OrderBy(a => a.DueDate)
                .Select(a => new DashboardDewormDueDto {
                    AppointmentId = a.AppointmentID,
                    DueDate = a.DueDate,
                    PetId = a.Pet.PetID,
                    PetName = a.Pet.Name,
                    ServiceType = a.ServiceCategory.ServiceType
                }).ToList();

            return Ok(new ApiResponse<DashboardResponse> {
                Success = true,
                Data = new DashboardResponse {
                    UserName = userName,
                    Pets = pets,
                    UpcomingAppointments = upcomingAppointments,
                    VaccineDue = vaccineDue,
                    DewormDue = dewormDue
                }
            });
        }
    }
}
