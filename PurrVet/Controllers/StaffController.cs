using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using PetCloud.Models;
using PetCloud.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PetCloudUser = PetCloud.Models.User;

namespace PetCloud.Controllers {
    public class StaffController : Controller {
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly SmsReminderService _smsService;
        private readonly EmailService _emailService;

        public StaffController(ApplicationDbContext context, GraphServiceClient graphServiceClient, IWebHostEnvironment hostEnvironment, SmsReminderService smsService, EmailService emailService) {
            _context = context;
            _graphServiceClient = graphServiceClient;
            _hostEnvironment = hostEnvironment;
            _smsService = smsService;
            _emailService = emailService;
        }
        [HttpGet]
        public IActionResult Dashboard() {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            var model = new DashboardViewModel {
                TotalUsers = _context.Users.Count(),
                TotalPets = _context.Pets.Count(),
                TotalAppointments = _context.Appointments.Count(),
                TotalCategory = _context.ServiceCategories.Count(),
                TotalType = _context.ServiceSubtypes.Count(),
                Years = Enumerable.Range(2020, DateTime.Now.Year - 2019).ToList()
            };

            return View(model);
        }
        [HttpGet]
        public JsonResult GetMonthlyTotals(string type, int year) {
            var monthlyData = Enumerable.Range(1, 12)
                .Select(m => new {
                    Month = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(m),
                    Count = type switch {
                        "Users" => _context.Users.Count(u => u.CreatedAt.Year == year && u.CreatedAt.Month == m),
                        "Appointments" => _context.Appointments.Count(a => a.AppointmentDate.Year == year && a.AppointmentDate.Month == m),
                        "Pets" => _context.Pets.Count(p => p.CreatedAt.Year == year && p.CreatedAt.Month == m),
                        _ => 0
                    }
                }).ToList();

            return Json(monthlyData);
        }
        [HttpGet]
        public JsonResult GetTotalsData(int year) {
            var data = new {
                users = year == 0 ? _context.Users.Count() : _context.Users.Count(u => u.CreatedAt.Year == year),
                pets = year == 0 ? _context.Pets.Count() : _context.Pets.Count(p => p.CreatedAt.Year == year),
                appointments = year == 0 ? _context.Appointments.Count() : _context.Appointments.Count(a => a.CreatedAt.Year == year)
            };

            return Json(data);
        }

        [HttpGet]
        public IActionResult Users(string searchQuery = "", string sortField = "", string sortOrder = "", string statusFilter = "", int page = 1) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            int pageSize = 10;

            if (string.IsNullOrEmpty(sortField))
                sortField = "id";
            if (string.IsNullOrEmpty(sortOrder))
                sortOrder = "asc";

            var query = _context.Users.AsQueryable();
            if (!string.IsNullOrEmpty(searchQuery)) {
                query = query.Where(u =>
                    u.FirstName.Contains(searchQuery) ||
                    u.LastName.Contains(searchQuery) ||
                    u.Email.Contains(searchQuery));
            }
            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All") {
                query = query.Where(u => u.Status == statusFilter);
            }
            query = (sortField, sortOrder) switch {
                ("id", "desc") => query.OrderByDescending(u => u.UserID),
                ("id", "asc") => query.OrderBy(u => u.UserID),

                ("fname", "desc") => query.OrderByDescending(u => u.FirstName),
                ("fname", "asc") => query.OrderBy(u => u.FirstName),

                ("lname", "desc") => query.OrderByDescending(u => u.LastName),
                ("lname", "asc") => query.OrderBy(u => u.LastName),

                ("email", "desc") => query.OrderByDescending(u => u.Email),
                ("email", "asc") => query.OrderBy(u => u.Email),

                ("type", "desc") => query.OrderByDescending(u => u.Type),
                ("type", "asc") => query.OrderBy(u => u.Type),

                _ => query.OrderBy(u => u.UserID)
            };

            int totalUsers = query.Count();
            int totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

            var users = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var viewModel = new UserListViewModel {
                Users = users,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchQuery = searchQuery,
                StatusFilter = statusFilter
            };

            ViewBag.SortField = sortField;
            ViewBag.SortOrder = sortOrder;

            return View(viewModel);
        }


        [HttpGet]
        public IActionResult AddUser() {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUser(PurrVetUser model, IFormFile? ProfileImage) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return Json(new { success = false, message = "Unauthorized access." });

            if (string.IsNullOrWhiteSpace(model.FirstName) ||
                string.IsNullOrWhiteSpace(model.LastName) ||
                string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(model.Phone) ||
                string.IsNullOrWhiteSpace(model.Password) ||
                string.IsNullOrWhiteSpace(model.Type)) {
                return Json(new { success = false, message = "All fields are required." });
            }

            if (_context.Users.Any(u => u.Email == model.Email))
                return Json(new { success = false, message = "Email already exists." });

            if (model.Phone.Length > 12)
                return Json(new { success = false, message = "Phone number cannot exceed 12 digits." });

            try {
                var hasher = new PasswordHasher<PurrVetUser>();
                model.Password = hasher.HashPassword(model, model.Password);
                model.Status = "Active";

                if (ProfileImage != null && ProfileImage.Length > 0) {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ProfileImage.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create)) {
                        await ProfileImage.CopyToAsync(stream);
                    }

                    model.ProfileImage = $"/uploads/profiles/{fileName}";
                } else {
                    model.ProfileImage = "/images/default-profile.png";
                }

                _context.Users.Add(model);
                await _context.SaveChangesAsync();

                if (model.Type == "Owner") {
                    var owner = new Owner {
                        UserID = model.UserID,
                        Name = $"{model.FirstName} {model.LastName}",
                        Email = model.Email,
                        Phone = model.Phone
                    };

                    _context.Owners.Add(owner);
                    await _context.SaveChangesAsync();
                }

                _context.Notifications.Add(new Notification {
                    Message = $"A new user '{model.FirstName} {model.LastName}' has been added.",
                    Type = "User",
                    RedirectUrl = $"/{{role}}/EditUser/{model.UserID}",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    TargetRole = "Staff"
                });
                _context.SystemLogs.Add(new SystemLog {
                    ActionType = "Create",
                    Module = "User",
                    Description = $"Created a new user: {model.FirstName} {model.LastName}",
                    PerformedBy = HttpContext.Session.GetString("UserName") ?? "Unknown",
                    Timestamp = DateTime.Now
                });

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "User added successfully!" });
            } catch (Exception ex) {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }


        [HttpGet]
        public IActionResult EditUser(int id) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            var user = _context.Users.FirstOrDefault(u => u.UserID == id);
            if (user == null)
                return NotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, string FirstName, string LastName, string Email,
                                           string Phone, string Password, string ConfirmPassword,
                                           string Type, string Status, IFormFile? ProfileImage) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return Json(new { success = false, message = "Unauthorized access." });

            var user = _context.Users.FirstOrDefault(u => u.UserID == id);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (string.IsNullOrWhiteSpace(FirstName) ||
                string.IsNullOrWhiteSpace(LastName) ||
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Phone) ||
                string.IsNullOrWhiteSpace(Type) ||
                string.IsNullOrWhiteSpace(Status)) {
                return Json(new { success = false, message = "All fields are required." });
            }

            if (_context.Users.Any(u => u.Email == Email && u.UserID != id))
                return Json(new { success = false, message = "Email already exists." });

            user.FirstName = FirstName;
            user.LastName = LastName;
            user.Email = Email;
            user.Phone = Phone;
            user.Type = Type;
            user.Status = Status;

            if (!string.IsNullOrWhiteSpace(Password)) {
                if (Password != ConfirmPassword)
                    return Json(new { success = false, message = "Passwords do not match." });

                var hasher = new PasswordHasher<PurrVetUser>();
                user.Password = hasher.HashPassword(user, Password);
            }

            if (ProfileImage != null && ProfileImage.Length > 0) {
                if (!string.IsNullOrEmpty(user.ProfileImage)) {
                    var oldPath = Path.Combine("wwwroot", user.ProfileImage.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ProfileImage.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create)) {
                    await ProfileImage.CopyToAsync(stream);
                }

                user.ProfileImage = $"/uploads/profiles/{fileName}";
            }

            try {
                _context.Users.Update(user);
                _context.SystemLogs.Add(new SystemLog {
                    ActionType = "Update",
                    Module = "User",
                    Description = $"Updated user: {user.FirstName} {user.LastName}",
                    PerformedBy = HttpContext.Session.GetString("UserName") ?? "Unknown",
                    Timestamp = DateTime.Now
                });

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "User updated successfully!" });
            } catch (Exception ex) {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult Appointments() {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            var appointments = _context.Appointments
                .Include(a => a.Pet)
                .Include(a => a.ServiceCategory)
                .Include(a => a.ServiceSubtype)
                .ToList();

            ViewBag.IsConnectedToMicrosoft = _context.MicrosoftAccountConnections.Any();

            return View(appointments);
        }
        [HttpGet]
        public IActionResult GetAppointmentsByGroup(int groupId) {
            var list = _context.Appointments
                .Where(a => a.GroupID == groupId && a.Status != "Completed")
                .Include(a => a.Pet)
                .Include(a => a.ServiceCategory)
                .Include(a => a.ServiceSubtype)
                .Select(a => new {
                    id = a.AppointmentID,
                    petName = a.Pet.Name,
                    category = a.ServiceCategory.ServiceType,
                    subtype = a.ServiceSubtype.ServiceSubType,
                    date = a.AppointmentDate.ToString("MMM dd yyyy hh:mm tt"),
                    status = a.Status
                })
                .ToList();

            return Json(list);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendReminderAgain([FromBody] ReminderRequest req) {
            var appointment = _context.Appointments
                .Include(a => a.Pet)
                    .ThenInclude(p => p.Owner)
                .Include(a => a.ServiceCategory)
                .FirstOrDefault(a => a.AppointmentID == req.AppointmentId);

            if (appointment == null)
                return Json(new { success = false, message = "Appointment not found." });

            if (appointment.ReminderCounterDate?.Date != DateTime.Today) {
                appointment.SmsSentToday = 0;
                appointment.EmailSentToday = 0;
                appointment.ReminderCounterDate = DateTime.Today;
            }

            var now = DateTime.Now;

            if (req.Channel.Contains("sms")) {
                if (appointment.LastSmsSentAt > now.AddHours(-6))
                    return Json(new { success = false, message = "SMS reminder can only be resent every 6 hours." });

                if (appointment.SmsSentToday >= 3)
                    return Json(new { success = false, message = "SMS reminder limit reached (3 per day)." });
            }

            if (req.Channel.Contains("email")) {
                if (appointment.LastEmailSentAt > now.AddHours(-1))
                    return Json(new { success = false, message = "Email reminder can only be resent every hour." });

                if (appointment.EmailSentToday >= 5)
                    return Json(new { success = false, message = "Email reminder limit reached (5 per day)." });
            }

            try {
                var phone = appointment.Pet?.Owner?.Phone;
                var email = appointment.Pet?.Owner?.Email;
                var petName = appointment.Pet?.Name ?? "your pet";
                var category = appointment.ServiceCategory?.ServiceType ?? "service";
                var date = appointment.AppointmentDate.ToString("MMM dd yyyy hh:mm tt");

                var smsMessage =
                    $"Good day! This is Happy Paws Vet Clinic. Reminder for {petName}'s {category} on {date}.";

                bool smsAttempted = false;
                bool smsSent = false;
                bool emailSent = false;
                if (req.Channel.Contains("sms") && !string.IsNullOrEmpty(phone)) {
                    smsAttempted = true;
                    smsSent = await _smsService.ScheduleReminder(phone, now.AddMinutes(1), smsMessage);

                    if (smsSent) {
                        appointment.LastSmsSentAt = now;
                        appointment.SmsSentToday++;
                    } else if (req.Channel == "sms") {
                        return Json(new {
                            success = false,
                            message = "SMS failed to send. Please check SMS credits."
                        });
                    }
                }
                if (req.Channel.Contains("email") && !string.IsNullOrEmpty(email)) {
                    var subject = $"Reminder: {petName}'s {category} appointment";
                    var htmlBody = $@"
                <p>Hi {appointment.Pet?.Owner?.Name ?? "Pet Parent"},</p>
                <p>This is <strong>Happy Paws Vet Clinic ??</strong>.</p>
                <p>Reminder for <strong>{petName}'s</strong> {category} appointment:</p>
                <p><b>{date}</b></p>
                <p>See you soon!</p>
                <hr/>
                <small>This is an automated reminder email.</small>";

                    await _emailService.SendEmailAsync(email, subject, smsMessage, htmlBody);

                    appointment.LastEmailSentAt = now;
                    appointment.EmailSentToday++;
                    emailSent = true;
                }

                if (req.Channel.Contains("sms") && string.IsNullOrEmpty(phone)
                    && req.Channel == "sms") {
                    return Json(new {
                        success = false,
                        message = "No phone number available for SMS reminder."
                    });
                }

                if (req.Channel.Contains("email") && string.IsNullOrEmpty(email)
                    && req.Channel == "email") {
                    return Json(new {
                        success = false,
                        message = "No email address available for Email reminder."
                    });
                }

                if (!smsSent && !emailSent) {
                    return Json(new {
                        success = false,
                        message = "Reminder could not be sent using the selected options."
                    });
                }


                await _context.SaveChangesAsync();

                return Json(new {
                    success = true,
                    message =
                        smsSent && emailSent ? "SMS and Email reminders sent successfully."
                      : emailSent && smsAttempted ? "Email sent, but SMS failed (no credits)."
                      : smsSent ? "SMS reminder sent successfully."
                      : "Email reminder sent successfully."
                });
            } catch (Exception ex) {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class ReminderRequest {
            public int AppointmentId { get; set; }
            public string Channel { get; set; }
        }



        public async Task<IActionResult> MarkAsCompleted([FromBody] List<AppointmentCompleteDTO> data) {
            foreach (var item in data) {
                var appt = await _context.Appointments.FindAsync(item.id);
                if (appt == null) continue;

                appt.Status = "Completed";
                appt.AdministeredBy = item.administeredBy;
                appt.DueDate = item.dueDate;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        public class AppointmentCompleteDTO {
            public int id { get; set; }
            public string administeredBy { get; set; }
            public DateTime? dueDate { get; set; }
        }



        [HttpGet]
        public IActionResult ManageAppointments(
      string searchQuery = "",
      string statusFilter = "",
      string sortField = "date",
      string sortOrder = "asc",
      int page = 1) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            var now = DateTime.Now;
            var pastAppointments = _context.Appointments
                .Where(a =>
                    a.AppointmentDate.AddMinutes(30) < now &&
                    a.Status != "Completed" &&
                    a.Status != "Cancelled" &&
                    a.Status != "Missed")
                .Include(a => a.Pet).ThenInclude(p => p.Owner)
                .ToList();

            if (pastAppointments.Any()) {
                foreach (var appt in pastAppointments) {
                    appt.Status = "Missed";

                    _context.Notifications.Add(new Notification {
                        Message = $"Appointment for {appt.Pet?.Name ?? "a pet"} scheduled on {appt.AppointmentDate:MMM dd, yyyy hh:mm tt} was marked as missed.",
                        Type = "Appointment",
                        RedirectUrl = $"/{{role}}/ViewPet/{appt.Pet?.PetID}",
                        CreatedAt = DateTime.Now,
                        IsRead = false,
                        TargetRole = "Staff"
                    });

                    _context.SystemLogs.Add(new SystemLog {
                        ActionType = "Auto-Mark Missed",
                        PerformedBy = "System",
                        Timestamp = DateTime.Now,
                        Description = $"Appointment ID {appt.AppointmentID} was automatically marked as missed.",
                        Module = "Appointments"
                    });
                }
                _context.SaveChanges();
            }

            var query = _context.Appointments
                .Include(a => a.Pet).ThenInclude(p => p.Owner)
                .Include(a => a.ServiceCategory)
                .Include(a => a.ServiceSubtype)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery)) {
                query = query.Where(a =>
                    a.Pet.Name.Contains(searchQuery) ||
                    a.ServiceCategory.ServiceType.Contains(searchQuery) ||
                    a.ServiceSubtype.ServiceSubType.Contains(searchQuery));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All") {
                query = query.Where(a => a.Status == statusFilter);
            }

            query = sortField.ToLower() switch {
                "id" => sortOrder == "desc"
                    ? query.OrderByDescending(a => a.AppointmentID)
                    : query.OrderBy(a => a.AppointmentID),

                "owner" => sortOrder == "desc"
                    ? query.OrderByDescending(a => a.Pet.Owner.Name)
                    : query.OrderBy(a => a.Pet.Owner.Name),

                "date" => sortOrder == "desc"
                    ? query.OrderByDescending(a => a.AppointmentDate)
                    : query.OrderBy(a => a.AppointmentDate),

                _ => query.OrderBy(a => a.AppointmentDate)
            };

            const int pageSize = 10;
            int totalAppointments = query.Count();
            int totalPages = (int)Math.Ceiling(totalAppointments / (double)pageSize);

            var pagedAppointments = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var viewModel = new AppointmentListViewModel {
                Appointments = pagedAppointments,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchQuery = searchQuery,
                StatusFilter = statusFilter
            };

            ViewBag.SortField = sortField;
            ViewBag.SortOrder = sortOrder;

            return View(viewModel);
        }


        [HttpGet]
        public IActionResult AppointmentDetails(int groupId) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            var group = _context.AppointmentGroups
                .Include(g => g.Appointments)
                    .ThenInclude(a => a.Pet)
                    .ThenInclude(p => p.Owner)
                .Include(g => g.Appointments)
                    .ThenInclude(a => a.ServiceCategory)
                .Include(g => g.Appointments)
                    .ThenInclude(a => a.ServiceSubtype)
                .FirstOrDefault(g => g.GroupID == groupId);

            if (group == null)
                return NotFound();

            var viewModel = new AppointmentGroupViewModel {
                GroupID = group.GroupID,
                Appointments = group.Appointments.ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Professional_Fee(string searchQuery = "", string statusFilter = "", string subtypeFilter = "", string sortOrder = "", int page = 1) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            int pageSize = 10;

            var query = _context.Appointments
                .Include(a => a.Pet)
                .ThenInclude(p => p.Owner)
                .Include(a => a.ServiceCategory)
                .Include(a => a.ServiceSubtype)
                .Where(a => a.CategoryID == 1)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery)) {
                query = query.Where(a =>
                    a.Pet.Name.Contains(searchQuery) ||
                    (a.ServiceSubtype != null && a.ServiceSubtype.ServiceSubType.Contains(searchQuery)) ||
                    (a.ServiceCategory != null && a.ServiceCategory.ServiceType.Contains(searchQuery)));
            }

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All") {
                query = query.Where(a => a.Status == statusFilter);
            }

            if (!string.IsNullOrEmpty(subtypeFilter) && subtypeFilter != "All") {
                query = query.Where(a => a.ServiceSubtype.ServiceSubType == subtypeFilter);
            }
            query = sortOrder == "asc"
              ? query.OrderBy(a => a.AppointmentDate)
              : query.OrderByDescending(a => a.AppointmentDate);
            int totalAppointments = query.Count();
            int totalPages = (int)Math.Ceiling(totalAppointments / (double)pageSize);

            var professionalAppointments = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var subtypes = _context.ServiceSubtypes
                .Where(s => s.CategoryID == 1)
                .Select(s => s.ServiceSubType)
                .Distinct()
                .ToList();

            var viewModel = new ProfessionalListViewModel {
                ProfessionalAppointments = professionalAppointments,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchQuery = searchQuery,
                StatusFilter = statusFilter,
                TotalRecords = totalAppointments,
                CategoryName = "Professional Fee / Consultation",
                ServiceSubtypes = subtypes,
                SubtypeFilter = subtypeFilter
            };
            ViewBag.SortOrder = sortOrder;
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Vaccination(string searchQuery = "", string statusFilter = "", string subtypeFilter = "", string sortOrder = "", int page = 1) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            int pageSize = 10;

            var query = _context.Appointments
                .Include(a => a.Pet)
                .ThenInclude(p => p.Owner)
                .Include(a => a.ServiceCategory)
                .Include(a => a.ServiceSubtype)
                .Where(a => a.CategoryID == 2)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery)) {
                query = query.Where(a =>
                    a.Pet.Name.Contains(searchQuery) ||
                    (a.ServiceSubtype != null && a.ServiceSubtype.ServiceSubType.Contains(searchQuery)) ||
                    (a.ServiceCategory != null && a.ServiceCategory.ServiceType.Contains(searchQuery)));
            }

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All") {
                query = query.Where(a => a.Status == statusFilter);
            }

            if (!string.IsNullOrEmpty(subtypeFilter) && subtypeFilter != "All") {
                query = query.Where(a => a.ServiceSubtype.ServiceSubType == subtypeFilter);
            }
            query = sortOrder == "asc"
                ? query.OrderBy(a => a.AppointmentDate)
                : query.OrderByDescending(a => a.AppointmentDate);
            int totalAppointments = query.Count();
            int totalPages = (int)Math.Ceiling(totalAppointments / (double)pageSize);

            var vaccinationAppointments = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var subtypes = _context.ServiceSubtypes
                .Where(s => s.CategoryID == 2)
                .Select(s => s.ServiceSubType)
                .Distinct()
                .ToList();

            var viewModel = new VaccinationListViewModel {
                Vaccination = vaccinationAppointments,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchQuery = searchQuery,
                StatusFilter = statusFilter,
                TotalRecords = totalAppointments,
                CategoryName = "Vaccination",
                ServiceSubtypes = subtypes,
                SubtypeFilter = subtypeFilter
            };
            ViewBag.SortOrder = sortOrder;
            return View(viewModel);
        }
        [HttpGet]
        public IActionResult Deworming_Preventives(string searchQuery = "", string statusFilter = "", string subtypeFilter = "", string sortOrder = "", int page = 1) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            int pageSize = 10;

            var query = _context.Appointments
                .Include(a => a.Pet)
                .ThenInclude(p => p.Owner)
                .Include(a => a.ServiceCategory)
                .Include(a => a.ServiceSubtype)
                .Where(a => a.CategoryID == 3)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery)) {
                query = query.Where(a =>
                    a.Pet.Name.Contains(searchQuery) ||
                    (a.ServiceSubtype != null && a.ServiceSubtype.ServiceSubType.Contains(searchQuery)) ||
                    (a.ServiceCategory != null && a.ServiceCategory.ServiceType.Contains(searchQuery)));
            }

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All") {
                query = query.Where(a => a.Status == statusFilter);
            }

            if (!string.IsNullOrEmpty(subtypeFilter) && subtypeFilter != "All") {
                query = query.Where(a => a.ServiceSubtype.ServiceSubType == subtypeFilter);
            }
            query = sortOrder == "asc"
                ? query.OrderBy(a => a.AppointmentDate)
                : query.OrderByDescending(a => a.AppointmentDate);
            int totalAppointments = query.Count();
            int totalPages = (int)Math.Ceiling(totalAppointments / (double)pageSize);

            var dewormingAppointments = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var subtypes = _context.ServiceSubtypes
                .Where(s => s.CategoryID == 3)
                .Select(s => s.ServiceSubType)
                .Distinct()
                .ToList();

            var viewModel = new DewormingListViewModel {
                Deworming = dewormingAppointments,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchQuery = searchQuery,
                StatusFilter = statusFilter,
                TotalRecords = totalAppointments,
                CategoryName = "Deworming & Preventives",
                ServiceSubtypes = subtypes,
                SubtypeFilter = subtypeFilter
            };
            ViewBag.SortOrder = sortOrder;
            return View(viewModel);
        }
        [HttpGet]
        public IActionResult Diagnostics_Laboratory(string searchQuery = "", string statusFilter = "", string subtypeFilter = "", string sortOrder = "", int page = 1) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            int pageSize = 10;

            var query = _context.Appointments
                .Include(a => a.Pet)
                .ThenInclude(p => p.Owner)
                .Include(a => a.ServiceCategory)
                .Include(a => a.ServiceSubtype)
                .Where(a => a.CategoryID == 4)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery)) {
                query = query.Where(a =>
                    a.Pet.Name.Contains(searchQuery) ||
                    (a.ServiceSubtype != null && a.ServiceSubtype.ServiceSubType.Contains(searchQuery)) ||
                    (a.ServiceCategory != null && a.ServiceCategory.ServiceType.Contains(searchQuery)));
            }

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All") {
                query = query.Where(a => a.Status == statusFilter);
            }

            if (!string.IsNullOrEmpty(subtypeFilter) && subtypeFilter != "All") {
                query = query.Where(a => a.ServiceSubtype.ServiceSubType == subtypeFilter);
            }
            query = sortOrder == "asc"
                ? query.OrderBy(a => a.AppointmentDate)
                : query.OrderByDescending(a => a.AppointmentDate);
            int totalAppointments = query.Count();
            int totalPages = (int)Math.Ceiling(totalAppointments / (double)pageSize);

            var diagnosticslaboratoryAppointments = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var subtypes = _context.ServiceSubtypes
                .Where(s => s.CategoryID == 4)
                .Select(s => s.ServiceSubType)
                .Distinct()
                .ToList();

            var viewModel = new DiagnosticsLaboratoryListViewModel {
                DiagnosticsLaboratory = diagnosticslaboratoryAppointments,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchQuery = searchQuery,
                StatusFilter = statusFilter,
                TotalRecords = totalAppointments,
                CategoryName = "Diagnostics & Laboratory Tests",
                ServiceSubtypes = subtypes,
                SubtypeFilter = subtypeFilter
            };
            ViewBag.SortOrder = sortOrder;
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Medication_Treatment(string searchQuery = "", string statusFilter = "", string subtypeFilter = "", string sortOrder = "", int page = 1) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            int pageSize = 10;

            var query = _context.Appointments
                .Include(a => a.Pet)
                .ThenInclude(p => p.Owner)
                .Include(a => a.ServiceCategory)
                .Include(a => a.ServiceSubtype)
                .Where(a => a.CategoryID == 5)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery)) {
                query = query.Where(a =>
                    a.Pet.Name.Contains(searchQuery) ||
                    (a.ServiceSubtype != null && a.ServiceSubtype.ServiceSubType.Contains(searchQuery)) ||
                    (a.ServiceCategory != null && a.ServiceCategory.ServiceType.Contains(searchQuery)));
            }

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All") {
                query = query.Where(a => a.Status == statusFilter);
            }

            if (!string.IsNullOrEmpty(subtypeFilter) && subtypeFilter != "All") {
                query = query.Where(a => a.ServiceSubtype.ServiceSubType == subtypeFilter);
            }
            query = sortOrder == "asc"
                ? query.OrderBy(a => a.AppointmentDate)
                : query.OrderByDescending(a => a.AppointmentDate);
            int totalAppointments = query.Count();
            int totalPages = (int)Math.Ceiling(totalAppointments / (double)pageSize);

            var medicationtreatmentAppointments = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var subtypes = _context.ServiceSubtypes
                .Where(s => s.CategoryID == 5)
                .Select(s => s.ServiceSubType)
                .Distinct()
                .ToList();

            var viewModel = new MedicationTreatmentListViewModel {
                MedicationTreatment = medicationtreatmentAppointments,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchQuery = searchQuery,
                StatusFilter = statusFilter,
                TotalRecords = totalAppointments,
                CategoryName = "Medication & Treatment",
                ServiceSubtypes = subtypes,
                SubtypeFilter = subtypeFilter
            };
            ViewBag.SortOrder = sortOrder;
            return View(viewModel);
        }
        [HttpGet]
        public IActionResult Surgery(string searchQuery = "", string statusFilter = "", string subtypeFilter = "", string sortOrder = "", int page = 1) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            int pageSize = 10;

            var query = _context.Appointments
                .Include(a => a.Pet)
                .ThenInclude(p => p.Owner)
                .Include(a => a.ServiceCategory)
                .Include(a => a.ServiceSubtype)
                .Where(a => a.CategoryID == 6)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery)) {
                query = query.Where(a =>
                    a.Pet.Name.Contains(searchQuery) ||
                    (a.ServiceSubtype != null && a.ServiceSubtype.ServiceSubType.Contains(searchQuery)) ||
                    (a.ServiceCategory != null && a.ServiceCategory.ServiceType.Contains(searchQuery)));
            }

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All") {
                query = query.Where(a => a.Status == statusFilter);
            }

            if (!string.IsNullOrEmpty(subtypeFilter) && subtypeFilter != "All") {
                query = query.Where(a => a.ServiceSubtype.ServiceSubType == subtypeFilter);
            }
            query = sortOrder == "asc"
                ? query.OrderBy(a => a.AppointmentDate)
                : query.OrderByDescending(a => a.AppointmentDate);
            int totalAppointments = query.Count();
            int totalPages = (int)Math.Ceiling(totalAppointments / (double)pageSize);

            var surgeryAppointments = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var subtypes = _context.ServiceSubtypes
                .Where(s => s.CategoryID == 6)
                .Select(s => s.ServiceSubType)
                .Distinct()
                .ToList();

            var viewModel = new SurgeryListViewModel {
                Surgery = surgeryAppointments,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchQuery = searchQuery,
                StatusFilter = statusFilter,
                TotalRecords = totalAppointments,
                CategoryName = "Surgery",
                ServiceSubtypes = subtypes,
                SubtypeFilter = subtypeFilter
            };
            ViewBag.SortOrder = sortOrder;
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Grooming_Wellness(string searchQuery = "", string statusFilter = "", string subtypeFilter = "", string sortOrder = "", int page = 1) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            int pageSize = 10;

            var query = _context.Appointments
                .Include(a => a.Pet)
                .ThenInclude(p => p.Owner)
                .Include(a => a.ServiceCategory)
                .Include(a => a.ServiceSubtype)
                .Where(a => a.CategoryID == 7)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery)) {
                query = query.Where(a =>
                    a.Pet.Name.Contains(searchQuery) ||
                    (a.ServiceSubtype != null && a.ServiceSubtype.ServiceSubType.Contains(searchQuery)) ||
                    (a.ServiceCategory != null && a.ServiceCategory.ServiceType.Contains(searchQuery)));
            }

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All") {
                query = query.Where(a => a.Status == statusFilter);
            }

            if (!string.IsNullOrEmpty(subtypeFilter) && subtypeFilter != "All") {
                query = query.Where(a => a.ServiceSubtype.ServiceSubType == subtypeFilter);
            }
            query = sortOrder == "asc"
                ? query.OrderBy(a => a.AppointmentDate)
                : query.OrderByDescending(a => a.AppointmentDate);
            int totalAppointments = query.Count();
            int totalPages = (int)Math.Ceiling(totalAppointments / (double)pageSize);

            var groomingwellnessAppointments = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var subtypes = _context.ServiceSubtypes
                .Where(s => s.CategoryID == 7)
                .Select(s => s.ServiceSubType)
                .Distinct()
                .ToList();

            var viewModel = new GroomingWellnessListViewModel {
                GroomingWellness = groomingwellnessAppointments,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchQuery = searchQuery,
                StatusFilter = statusFilter,
                TotalRecords = totalAppointments,
                CategoryName = "Grooming & Wellness",
                ServiceSubtypes = subtypes,
                SubtypeFilter = subtypeFilter
            };
            ViewBag.SortOrder = sortOrder;
            return View(viewModel);
        }
        [HttpGet]
        public IActionResult Confinement_Hospitalization(string searchQuery = "", string statusFilter = "", string subtypeFilter = "", string sortOrder = "", int page = 1) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            int pageSize = 10;

            var query = _context.Appointments
                .Include(a => a.Pet)
                .ThenInclude(p => p.Owner)
                .Include(a => a.ServiceCategory)
                .Include(a => a.ServiceSubtype)
                .Where(a => a.CategoryID == 8)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery)) {
                query = query.Where(a =>
                    a.Pet.Name.Contains(searchQuery) ||
                    (a.ServiceSubtype != null && a.ServiceSubtype.ServiceSubType.Contains(searchQuery)) ||
                    (a.ServiceCategory != null && a.ServiceCategory.ServiceType.Contains(searchQuery)));
            }

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All") {
                query = query.Where(a => a.Status == statusFilter);
            }

            if (!string.IsNullOrEmpty(subtypeFilter) && subtypeFilter != "All") {
                query = query.Where(a => a.ServiceSubtype.ServiceSubType == subtypeFilter);
            }
            query = sortOrder == "asc"
                ? query.OrderBy(a => a.AppointmentDate)
                : query.OrderByDescending(a => a.AppointmentDate);
            int totalAppointments = query.Count();
            int totalPages = (int)Math.Ceiling(totalAppointments / (double)pageSize);

            var confinementhospitalizationAppointments = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var subtypes = _context.ServiceSubtypes
                .Where(s => s.CategoryID == 8)
                .Select(s => s.ServiceSubType)
                .Distinct()
                .ToList();

            var viewModel = new ConfinementHospitalizationListViewModel {
                ConfinementHospitalization = confinementhospitalizationAppointments,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchQuery = searchQuery,
                StatusFilter = statusFilter,
                TotalRecords = totalAppointments,
                CategoryName = "Confinement / Hospitalization",
                ServiceSubtypes = subtypes,
                SubtypeFilter = subtypeFilter
            };
            ViewBag.SortOrder = sortOrder;
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult SpecialtyTests(string searchQuery = "", string statusFilter = "", string subtypeFilter = "", string sortOrder = "", int page = 1) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            int pageSize = 10;

            var query = _context.Appointments
                .Include(a => a.Pet)
                .ThenInclude(p => p.Owner)
                .Include(a => a.ServiceCategory)
                .Include(a => a.ServiceSubtype)
                .Where(a => a.CategoryID == 9)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery)) {
                query = query.Where(a =>
                    a.Pet.Name.Contains(searchQuery) ||
                    (a.ServiceSubtype != null && a.ServiceSubtype.ServiceSubType.Contains(searchQuery)) ||
                    (a.ServiceCategory != null && a.ServiceCategory.ServiceType.Contains(searchQuery)));
            }

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All") {
                query = query.Where(a => a.Status == statusFilter);
            }

            if (!string.IsNullOrEmpty(subtypeFilter) && subtypeFilter != "All") {
                query = query.Where(a => a.ServiceSubtype.ServiceSubType == subtypeFilter);
            }
            query = sortOrder == "asc"
                ? query.OrderBy(a => a.AppointmentDate)
                : query.OrderByDescending(a => a.AppointmentDate);
            int totalAppointments = query.Count();
            int totalPages = (int)Math.Ceiling(totalAppointments / (double)pageSize);

            var specialtytestAppointments = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var subtypes = _context.ServiceSubtypes
                .Where(s => s.CategoryID == 9)
                .Select(s => s.ServiceSubType)
                .Distinct()
                .ToList();

            var viewModel = new SpecialtyTestsListViewModel {
                SpecialtyTest = specialtytestAppointments,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchQuery = searchQuery,
                StatusFilter = statusFilter,
                TotalRecords = totalAppointments,
                CategoryName = "Specialty Tests / Rare Cases",
                ServiceSubtypes = subtypes,
                SubtypeFilter = subtypeFilter
            };
            ViewBag.SortOrder = sortOrder;
            return View(viewModel);
        }
        [HttpGet]
        public IActionResult EndLife(string searchQuery = "", string statusFilter = "", string subtypeFilter = "", string sortOrder = "", int page = 1) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            int pageSize = 10;

            var query = _context.Appointments
                .Include(a => a.Pet)
                .ThenInclude(p => p.Owner)
                .Include(a => a.ServiceCategory)
                .Include(a => a.ServiceSubtype)
                .Where(a => a.CategoryID == 10)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery)) {
                query = query.Where(a =>
                    a.Pet.Name.Contains(searchQuery) ||
                    (a.ServiceSubtype != null && a.ServiceSubtype.ServiceSubType.Contains(searchQuery)) ||
                    (a.ServiceCategory != null && a.ServiceCategory.ServiceType.Contains(searchQuery)));
            }

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All") {
                query = query.Where(a => a.Status == statusFilter);
            }

            if (!string.IsNullOrEmpty(subtypeFilter) && subtypeFilter != "All") {
                query = query.Where(a => a.ServiceSubtype.ServiceSubType == subtypeFilter);
            }
            query = sortOrder == "asc"
                ? query.OrderBy(a => a.AppointmentDate)
                : query.OrderByDescending(a => a.AppointmentDate);
            int totalAppointments = query.Count();
            int totalPages = (int)Math.Ceiling(totalAppointments / (double)pageSize);

            var endLlifeAppointments = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var subtypes = _context.ServiceSubtypes
                .Where(s => s.CategoryID == 10)
                .Select(s => s.ServiceSubType)
                .Distinct()
                .ToList();

            var viewModel = new EndLifeListViewModel {
                EndLife = endLlifeAppointments,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchQuery = searchQuery,
                StatusFilter = statusFilter,
                TotalRecords = totalAppointments,
                CategoryName = "End of Life Care",
                ServiceSubtypes = subtypes,
                SubtypeFilter = subtypeFilter
            };
            ViewBag.SortOrder = sortOrder;
            return View(viewModel);
        }
        [HttpGet]
        public IActionResult AddAppointment() {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            ViewBag.Pets = _context.Pets
                .Include(p => p.Owner)
                .Select(p => new {
                    p.PetID,
                    p.Name,
                    p.Type,
                    Owner = new {
                        p.Owner.Name,
                        p.Owner.Email,
                        p.Owner.Phone
                    }
                })
                .ToList();

            ViewBag.ServiceCategories = _context.ServiceCategories
                .Select(c => new {
                    c.CategoryID,
                    c.ServiceType
                })
                .ToList();

            ViewBag.ServiceSubtypes = _context.ServiceSubtypes
                .Select(s => new {
                    s.SubtypeID,
                    s.ServiceSubType,
                    s.CategoryID
                })
                .ToList();
            ViewBag.Owners = _context.Owners
                .Select(o => new { o.OwnerID, o.Name })
                .ToList();

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddAppointment(Appointment model, string AppointmentTime) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return Json(new { success = false, message = "Unauthorized access." });

            if (model.PetID == 0 || model.CategoryID == null || model.AppointmentDate == default)
                return Json(new { success = false, message = "All required fields must be filled." });

            if (TimeSpan.TryParse(AppointmentTime, out var parsedTime))
                model.AppointmentDate = model.AppointmentDate.Date.Add(parsedTime);

            bool taken = _context.Appointments.Any(a => a.AppointmentDate == model.AppointmentDate);
            if (taken)
                return Json(new { success = false, message = "This time slot is already taken." });

            model.Status = "Pending";

            try {
                _context.Appointments.Add(model);
                _context.Notifications.Add(new Notification {
                    Message = $"A new appointment for Pet ID: {model.PetID} has been created on {model.AppointmentDate:MMM dd, yyyy hh:mm tt}.",
                    Type = "Appointment",
                    RedirectUrl = $"/{{role}}/ViewPet/{model.PetID}",
                    TargetRole = "Staff"
                });
                _context.SystemLogs.Add(new SystemLog {
                    ActionType = "Create",
                    Module = "Appointment",
                    Description = $"Created an appointment: {model.AppointmentID}, {model.AppointmentDate}",
                    PerformedBy = HttpContext.Session.GetString("UserName") ?? "Unknown",
                    Timestamp = DateTime.Now
                });
                _context.SaveChanges();
                return Json(new { success = true, message = "Appointment added successfully!" });
            } catch (Exception ex) {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAppointmentsBulk([FromForm] AppointmentBulkForm form) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return Json(new { success = false, message = "Unauthorized access." });

            if (form?.Appointments == null || !form.Appointments.Any())
                return Json(new { success = false, message = "No appointments provided." });

            var invalidIndex = form.Appointments
                .Select((a, idx) => new { a, idx })
                .FirstOrDefault(x =>
                    x.a.PetID == 0 ||
                    x.a.CategoryID == null ||
                    x.a.SubtypeID == null ||
                    x.a.AppointmentDate == default ||
                    string.IsNullOrWhiteSpace(x.a.AppointmentTime));

            if (invalidIndex != null)
                return Json(new { success = false, message = $"Item #{invalidIndex.idx + 1} is missing required fields." });

            var appointmentDateTimes = new List<DateTime>();
            for (int i = 0; i < form.Appointments.Count; i++) {
                var item = form.Appointments[i];
                if (!TimeSpan.TryParse(item.AppointmentTime, out var ts))
                    return Json(new { success = false, message = $"Invalid time format for item #{i + 1}." });

                appointmentDateTimes.Add(item.AppointmentDate.Date.Add(ts));
            }

            for (int i = 0; i < appointmentDateTimes.Count; i++) {
                var dt = appointmentDateTimes[i];
                bool taken = _context.Appointments.Any(a => a.AppointmentDate == dt);
                if (taken)
                    return Json(new { success = false, message = $"Time slot {dt:MMM dd, yyyy hh:mm tt} is already taken (item #{i + 1})." });
            }

            using var tx = _context.Database.BeginTransaction();
            try {
                var groupDateTime = appointmentDateTimes.First();
                var group = new AppointmentGroup {
                    GroupTime = groupDateTime,
                    Notes = "Grouped appointment",
                    Status = "Draft",
                    CreatedAt = DateTime.Now
                };
                _context.AppointmentGroups.Add(group);
                await _context.SaveChangesAsync();

                var added = new List<Appointment>();
                for (int i = 0; i < form.Appointments.Count; i++) {
                    var item = form.Appointments[i];
                    var appt = new Appointment {
                        PetID = item.PetID,
                        CategoryID = item.CategoryID,
                        SubtypeID = item.SubtypeID,
                        AppointmentDate = group.GroupTime,
                        Notes = item.Notes ?? "No notes.",
                        Status = "Pending",
                        GroupID = group.GroupID,
                        CreatedAt = DateTime.Now
                    };
                    _context.Appointments.Add(appt);
                    added.Add(appt);
                }

                await _context.SaveChangesAsync();

                var userName = HttpContext.Session.GetString("UserName") ?? "Unknown";
                var dateText = groupDateTime.ToString("MMM dd, yyyy hh:mm tt");

                _context.Notifications.Add(new Notification {
                    Message = $"A new group appointment (#{group.GroupID}) was created for {dateText}, containing {added.Count} appointments.",
                    Type = "Appointment",
                    TargetRole = "Staff"
                });

                foreach (var appt in added) {
                    _context.Notifications.Add(new Notification {
                        Message = $"New appointment for Pet ID: {appt.PetID} on {dateText}.",
                        Type = "Appointment",
                        TargetRole = "Staff"
                    });

                    _context.SystemLogs.Add(new SystemLog {
                        ActionType = "Create",
                        Module = "Appointment",
                        Description = $"Created appointment for Pet ID {appt.PetID} (Group #{group.GroupID}) on {dateText}.",
                        PerformedBy = userName,
                        Timestamp = DateTime.Now
                    });
                }

                _context.SystemLogs.Add(new SystemLog {
                    ActionType = "Bulk Create",
                    Module = "Appointment",
                    Description = $"Created {added.Count} services in Group #{group.GroupID} scheduled for {dateText}.",
                    PerformedBy = userName,
                    Timestamp = DateTime.Now
                });

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                _ = Task.Run(async () => {
                    try {
                        foreach (var appt in added) {
                            var pet = _context.Pets.Include(p => p.Owner).FirstOrDefault(p => p.PetID == appt.PetID);
                            if (pet == null) continue;
                            var category = _context.ServiceCategories.FirstOrDefault(c => c.CategoryID == appt.CategoryID);
                            var subtype = _context.ServiceSubtypes.FirstOrDefault(s => s.SubtypeID == appt.SubtypeID);
                            var ownerName = pet.Owner?.Name ?? "Pet Parent";
                            var phone = pet.Owner?.Phone;
                            var email = pet.Owner?.Email;
                            if (string.IsNullOrWhiteSpace(phone)) continue;
                            string message = $"?? Good Day, {ownerName}! This is Happy Paws Veterinary Clinic. You have an upcoming appointment on {appt.AppointmentDate:MMMM dd, yyyy (dddd)} at {appt.AppointmentDate:hh:mm tt}. Service: {category?.ServiceType ?? "General"} - {subtype?.ServiceSubType ?? "Consultation"}.";
                            DateTime reminder5 = appt.AppointmentDate.AddDays(-5).Date.AddHours(8);
                            DateTime reminder3 = appt.AppointmentDate.AddDays(-3).Date.AddHours(8);
                            if (reminder5 > DateTime.Now) await _smsService.ScheduleReminder(phone, reminder5, message);
                            if (reminder3 > DateTime.Now) await _smsService.ScheduleReminder(phone, reminder3, message);
                            if (!string.IsNullOrEmpty(email)) {
                                var subject = $"Upcoming Appointment for {pet.Name}";
                                await _emailService.SendEmailAsync(email, subject, message, message);
                            }
                        }
                    } catch (Exception ex) {
                        Console.WriteLine("[Reminder scheduling error] " + ex.Message);
                    }
                });

                return Json(new { success = true, message = $"{added.Count} services added (Group #{group.GroupID})!", groupId = group.GroupID });
            } catch (Exception ex) {
                await tx.RollbackAsync();
                return Json(new { success = false, message = $"Error saving group: {ex.Message}" });
            }
        }



        [HttpGet]
        public IActionResult GetPetsByOwner(int ownerId) {
            var pets = _context.Pets
                .Include(p => p.Owner)
                .Where(p => p.OwnerID == ownerId)
                .Select(p => new {
                    p.PetID,
                    p.Name,
                    p.Type
                })
                .ToList();

            return Json(pets);
        }


        [HttpGet]
        public IActionResult EditAppointment(int id) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            var appointment = _context.Appointments
                .Include(a => a.Pet)
                .Include(a => a.ServiceCategory)
                .Include(a => a.ServiceSubtype)
                .FirstOrDefault(a => a.AppointmentID == id);

            if (appointment == null)
                return NotFound();

            ViewBag.Pets = _context.Pets
                .Include(p => p.Owner)
                .Select(p => new {
                    p.PetID,
                    p.Name,
                    p.Type,
                    Owner = new { p.Owner.Name }
                })
                .ToList();

            ViewBag.ServiceCategories = _context.ServiceCategories
                .Select(c => new {
                    c.CategoryID,
                    c.ServiceType
                })
                .ToList();

            var subtypes = _context.ServiceSubtypes
                .Select(s => new {
                    s.SubtypeID,
                    s.ServiceSubType,
                    s.CategoryID
                })
                .ToList();

            ViewBag.ServiceSubtypesJson = JsonSerializer.Serialize(subtypes);

            return View(appointment);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditAppointment(Appointment model, string AppointmentTime) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return Json(new { success = false, message = "Unauthorized access." });

            var existing = _context.Appointments.FirstOrDefault(a => a.AppointmentID == model.AppointmentID);
            if (existing == null)
                return Json(new { success = false, message = "Appointment not found." });

            if (TimeSpan.TryParse(AppointmentTime, out var parsedTime))
                model.AppointmentDate = model.AppointmentDate.Date.Add(parsedTime);

            bool taken = _context.Appointments.Any(a =>
                   a.AppointmentDate == model.AppointmentDate &&
                   a.AppointmentID != model.AppointmentID &&
                   (existing.GroupID == null || a.GroupID != existing.GroupID)
               );

            if (taken)
                return Json(new { success = false, message = "This time slot is already taken." });

            existing.PetID = model.PetID;
            existing.CategoryID = model.CategoryID;
            existing.SubtypeID = model.SubtypeID;
            existing.AppointmentDate = model.AppointmentDate;
            existing.Status = model.Status;
            existing.Notes = model.Notes;

            if (existing.GroupID != null) {
                var groupAppointments = _context.Appointments
                    .Where(a => a.GroupID == existing.GroupID && a.AppointmentID != existing.AppointmentID)
                    .ToList();

                foreach (var appt in groupAppointments) {
                    appt.AppointmentDate = model.AppointmentDate;
                    _context.Appointments.Update(appt);
                }
            }

            try {
                _context.Appointments.Update(existing);

                var pet = _context.Pets
                    .Include(p => p.Owner)
                    .FirstOrDefault(p => p.PetID == existing.PetID);

                if (pet != null && (model.Status == "Cancelled" || model.Status == "Missed" || model.Status == "Completed" || model.Status == "Pending")) {
                    string message = model.Status == "Pending"
                        ? $"A new appointment has been requested by {pet.Owner.Name} for their pet {pet.Name}."
                        : $"An appointment for {pet.Name} ({pet.Owner.Name}) has been marked as {model.Status}.";

                    _context.Notifications.Add(new Notification {
                        Message = message,
                        Type = "Appointment",
                        CreatedAt = DateTime.Now,
                        IsRead = false,
                        TargetRole = "Staff"
                    });
                }

                _context.SystemLogs.Add(new SystemLog {
                    ActionType = "Update",
                    Module = "Appointment",
                    Description = existing.GroupID != null
                        ? $"Updated all appointments in group {existing.GroupID}."
                        : $"Updated appointment ID:{model.AppointmentID} ({model.AppointmentDate})",
                    PerformedBy = HttpContext.Session.GetString("UserName") ?? "Unknown",
                    Timestamp = DateTime.Now
                });

                _context.SaveChanges();

                return Json(new { success = true, message = existing.GroupID != null ? "Service updated successfully!" : "Service updated successfully!" });
            } catch (Exception ex) {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
        [HttpGet]
        public IActionResult EditAppointmentsGroup(int groupId) {
            var group = _context.AppointmentGroups
                .Include(g => g.Appointments)
                    .ThenInclude(a => a.Pet)
                .Include(g => g.Appointments)
                    .ThenInclude(a => a.ServiceCategory)
                .Include(g => g.Appointments)
                    .ThenInclude(a => a.ServiceSubtype)
                .FirstOrDefault(g => g.GroupID == groupId);

            if (group == null)
                return NotFound();

            ViewBag.ServiceCategories = _context.ServiceCategories.ToList();
            ViewBag.ServiceSubtypes = _context.ServiceSubtypes.ToList();
            ViewBag.Owners = _context.Owners.ToList();

            return View("EditAppointmentsGroup", group);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditAppointmentsGroup(int groupId, string GroupDate, string GroupTime, List<Appointment> appointments) {
            var group = _context.AppointmentGroups
                .Include(g => g.Appointments)
                .FirstOrDefault(g => g.GroupID == groupId);

            if (group == null)
                return Json(new { success = false, message = "Group not found." });

            var role = HttpContext.Session.GetString("UserRole");
            if (group.Status == "Finalized" && role != "Staff")
                return Json(new { success = false, message = "This group is finalized and cannot be edited by owners." });

            if (!TimeSpan.TryParse(GroupTime, out var parsedTime))
                return Json(new { success = false, message = "Invalid time selected." });

            if (!DateTime.TryParse(GroupDate, out var parsedDate))
                return Json(new { success = false, message = "Invalid date selected." });

            var newGroupDateTime = parsedDate.Date.Add(parsedTime);

            bool appointmentConflict = _context.Appointments
                .Any(a => a.AppointmentDate == newGroupDateTime
                       && a.GroupID != groupId);

            var drafts = _context.AppointmentDrafts
                .Where(d => d.GroupDraftId != group.GroupID.ToString())
                .ToList();

            bool draftConflict = drafts.Any(d => {
                if (d.AppointmentTime != null && TimeSpan.TryParse(d.AppointmentTime, out var dTime)) {
                    return d.AppointmentDate.Date.Add(dTime) == newGroupDateTime;
                } else {
                    return d.AppointmentDate == newGroupDateTime;
                }
            });



            if (appointmentConflict || draftConflict) {
                return Json(new {
                    success = false,
                    message = "Selected date and time conflicts with an existing appointment or draft."
                });
            }


            group.GroupTime = newGroupDateTime;

            var userName = HttpContext.Session.GetString("UserName") ?? "Unknown";
            int updatedCount = 0, addedCount = 0, removedCount = 0;

            using var tx = _context.Database.BeginTransaction();

            try {
                var cleanAppointments = appointments
                    .Where(a => a != null && a.PetID != 0 && a.CategoryID != null)
                    .ToList();

                var postedExistingIds = cleanAppointments
                    .Where(a => a.AppointmentID > 0)
                    .Select(a => a.AppointmentID)
                    .ToList();

                foreach (var appt in cleanAppointments) {
                    if (appt.AppointmentID > 0) {
                        var existing = group.Appointments.First(a => a.AppointmentID == appt.AppointmentID);
                        _context.Entry(existing).CurrentValues.SetValues(appt);
                        existing.GroupID = group.GroupID;
                        existing.AppointmentDate = group.GroupTime;
                        updatedCount++;
                    } else {
                        var newAppt = new Appointment {
                            PetID = appt.PetID,
                            CategoryID = appt.CategoryID,
                            SubtypeID = appt.SubtypeID,
                            Notes = appt.Notes ?? "No notes.",
                            Status = appt.Status ?? "Pending",
                            GroupID = group.GroupID,
                            AppointmentDate = group.GroupTime,
                            CreatedAt = DateTime.Now
                        };
                        _context.Appointments.Add(newAppt);
                        addedCount++;
                    }
                }

                var toRemove = group.Appointments
                    .Where(a => a.AppointmentID > 0 && !postedExistingIds.Contains(a.AppointmentID))
                    .ToList();

                foreach (var appt in toRemove) {
                    _context.Appointments.Remove(appt);
                    removedCount++;
                }

                _context.SystemLogs.Add(new SystemLog {
                    ActionType = "Update",
                    Module = "Appointment",
                    Description = $"Edited {updatedCount}, added {addedCount}, removed {removedCount} in group #{groupId}.",
                    PerformedBy = userName,
                    Timestamp = DateTime.Now
                });

                _context.SaveChanges();
                tx.Commit();

                return Json(new {
                    success = true,
                    message = $"Group #{groupId} updated � {updatedCount} modified, {addedCount} added, {removedCount} removed."
                });
            } catch (Exception ex) {
                tx.Rollback();
                return Json(new { success = false, message = $"Error updating group: {ex.Message}" });
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FinalizeAppointmentsGroup([FromBody] List<string> groupIds) {
            if (groupIds == null || !groupIds.Any())
                return Json(new { success = false, message = "No group IDs provided." });

            foreach (var groupIdStr in groupIds) {
                if (int.TryParse(groupIdStr, out var groupId)) {
                    var group = _context.AppointmentGroups.FirstOrDefault(g => g.GroupID == groupId);
                    if (group != null && group.Status != "Finalized") {
                        group.Status = "Finalized";
                        group.FinalizedAt = DateTime.Now;
                        _context.SystemLogs.Add(new SystemLog {
                            ActionType = "Update",
                            Module = "Appointment",
                            Description = $"Finalized appointment group #{groupId}.",
                            PerformedBy = HttpContext.Session.GetString("UserName") ?? "Unknown",
                            Timestamp = DateTime.Now
                        });
                    }
                } else {
                    Console.WriteLine($"Skipped draft group {groupIdStr}");
                }
            }

            _context.SaveChanges();
            return Json(new { success = true, message = "Groups finalized." });
        }


        [HttpGet]
        public IActionResult GetDraftCart() {
            var userId = HttpContext.Session.GetInt32("UserID");
            var ownerId = HttpContext.Session.GetInt32("OwnerID");
            var role = HttpContext.Session.GetString("UserRole");

            IQueryable<AppointmentDraft> query = _context.AppointmentDrafts
                .Include(d => d.Pet)
                .OrderByDescending(d => d.CreatedAt);

            if (role == "Owner" && ownerId != null)
                query = query.Where(d => d.OwnerID == ownerId);
            else if (userId != null)
                query = query.Where(d => d.UserID == userId);
            else
                return Json(new { success = false, message = "Unauthorized access." });

            return Json(new { success = true, items = query.ToList() });
        }

        [HttpPost]
        public IActionResult RemoveDraft(int draftId) {
            var draft = _context.AppointmentDrafts.FirstOrDefault(d => d.DraftID == draftId);
            if (draft == null)
                return Json(new { success = false, message = "Item not found." });

            _context.AppointmentDrafts.Remove(draft);
            _context.SaveChanges();

            return Json(new { success = true, message = "Draft removed." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveAppointmentDrafts([FromBody] List<AppointmentDraft> drafts) {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserID");
            var ownerId = HttpContext.Session.GetInt32("OwnerID");

            if (userId == null && ownerId == null)
                return Json(new { success = false, message = "Unauthorized access." });

            if (drafts == null || !drafts.Any())
                return Json(new { success = false, message = "No drafts received." });

            try {
                var notifications = new List<Notification>();

                foreach (var draft in drafts) {
                    if (draft.PetID == null || draft.CategoryID == null || draft.AppointmentDate == default)
                        continue;

                    if (string.IsNullOrWhiteSpace(draft.GroupDraftId)) {
                        draft.GroupDraftId = Guid.NewGuid().ToString();
                    }

                    var exists = _context.AppointmentDrafts.Any(d =>
                         d.PetID == draft.PetID &&
                         d.CategoryID == draft.CategoryID &&
                         d.SubtypeID == draft.SubtypeID &&
                         d.AppointmentDate == draft.AppointmentDate &&
                         d.AppointmentTime == draft.AppointmentTime &&
                         d.GroupDraftId == draft.GroupDraftId
                     );

                    if (exists) continue;

                    var newDraft = new AppointmentDraft {
                        UserID = userId,
                        OwnerID = ownerId ?? draft.OwnerID,
                        PetID = draft.PetID,
                        CategoryID = draft.CategoryID,
                        SubtypeID = draft.SubtypeID,
                        AppointmentDate = draft.AppointmentDate,
                        AppointmentTime = draft.AppointmentTime,
                        Notes = draft.Notes ?? "No notes.",
                        CreatedAt = DateTime.Now,
                        GroupDraftId = draft.GroupDraftId
                    };

                    _context.AppointmentDrafts.Add(newDraft);
                }

                _context.SaveChanges();

                if (notifications.Any()) {
                    _context.Notifications.AddRange(notifications);
                    _context.SaveChanges();
                }

                return Json(new { success = true, message = "Drafts saved successfully." });
            } catch (Exception ex) {
                return Json(new { success = false, message = $"Error saving drafts: {ex.Message}" });
            }
        }


        public class SaveDraftsToAppointmentsRequest {
            public List<string> GroupDraftIds { get; set; } = new List<string>();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveDraftsToAppointments([FromBody] SaveDraftsToAppointmentsRequest request) {
            var userId = HttpContext.Session.GetInt32("UserID");
            var ownerId = HttpContext.Session.GetInt32("OwnerID");

            if (userId == null && ownerId == null)
                return Json(new { success = false, message = "Unauthorized access." });

            if (request == null || request.GroupDraftIds == null || !request.GroupDraftIds.Any())
                return Json(new { success = false, message = "No groupDraftIds provided." });

            var createdGroupIds = new List<int>();
            var notifications = new List<Notification>();

            using var tx = _context.Database.BeginTransaction();
            try {
                foreach (var groupDraftId in request.GroupDraftIds.Distinct()) {
                    if (string.IsNullOrWhiteSpace(groupDraftId))
                        continue;

                    var drafts = _context.AppointmentDrafts
                        .Where(d => d.GroupDraftId == groupDraftId
                                    && d.UserID == userId
                                    && d.OwnerID != null)
                        .ToList();

                    if (!drafts.Any())
                        continue;

                    var first = drafts.First();
                    var groupTime = first.AppointmentDate;
                    if (!string.IsNullOrEmpty(first.AppointmentTime) &&
                        TimeSpan.TryParse(first.AppointmentTime, out var ts)) {
                        groupTime = first.AppointmentDate.Date.Add(ts);
                    }

                    var group = new AppointmentGroup {
                        GroupTime = groupTime,
                        Notes = $"Converted from draft group {groupDraftId}",
                        Status = "Pending",
                        CreatedAt = DateTime.Now
                    };
                    _context.AppointmentGroups.Add(group);
                    _context.SaveChanges();

                    foreach (var draft in drafts) {
                        var exists = _context.Appointments.Any(a =>
                            a.PetID == draft.PetID &&
                            a.CategoryID == draft.CategoryID &&
                            a.SubtypeID == draft.SubtypeID &&
                            a.GroupID == group.GroupID
                        );
                        if (exists) continue;

                        var appointment = new Appointment {
                            PetID = draft.PetID.Value,
                            CategoryID = draft.CategoryID,
                            SubtypeID = draft.SubtypeID,
                            AppointmentDate = group.GroupTime,
                            Notes = draft.Notes ?? "No notes.",
                            Status = "Pending",
                            GroupID = group.GroupID,
                            CreatedAt = DateTime.Now
                        };

                        _context.Appointments.Add(appointment);

                        var pet = _context.Pets
                            .Include(p => p.Owner)
                            .FirstOrDefault(p => p.PetID == draft.PetID);

                        if (pet?.Owner != null) {
                            notifications.Add(new Notification {
                                Message = $"Your pet has an upcoming appointment on {group.GroupTime:MMM dd, yyyy hh:mm tt}.",
                                TargetUserId = pet.Owner.UserID,
                                TargetRole = "Owner",
                                RedirectUrl = "/Owner/Appointments",
                                Type = "Appointment",
                                CreatedAt = DateTime.Now,
                                IsRead = false
                            });
                        }

                        notifications.Add(new Notification {
                            Message = $"A new appointment for Pet ID: {draft.PetID} has been created on {group.GroupTime:MMM dd, yyyy hh:mm tt}.",
                            RedirectUrl = $"/{{role}}/ViewPet/{draft.PetID}",
                            Type = "Appointment",
                            CreatedAt = DateTime.Now,
                            IsRead = false,
                            TargetRole = "Staff"
                        });
                    }


                    _context.AppointmentDrafts.RemoveRange(drafts);
                    _context.SaveChanges();

                    createdGroupIds.Add(group.GroupID);
                }

                if (notifications.Any()) {
                    _context.Notifications.AddRange(notifications);
                    _context.SaveChanges();
                }

                tx.Commit();

                if (!createdGroupIds.Any())
                    return Json(new { success = false, message = "No drafts were converted." });

                return Json(new {
                    success = true,
                    message = "Drafts converted to appointments!",
                    groupIds = createdGroupIds
                });
            } catch (Exception ex) {
                tx.Rollback();
                return Json(new { success = false, message = $"Error saving drafts: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult EditDraftGroup(string draftId) {
            var userId = HttpContext.Session.GetInt32("UserID");
            var sessionOwnerId = HttpContext.Session.GetInt32("OwnerID");

            if (string.IsNullOrEmpty(draftId))
                return NotFound();


            var drafts = _context.AppointmentDrafts
                .Where(d => d.GroupDraftId == draftId &&
                            ((sessionOwnerId != null && d.OwnerID == sessionOwnerId) ||
                             (userId != null && d.UserID == userId)))
                .Include(d => d.Pet)
                    .ThenInclude(p => p.Owner)
                .OrderBy(d => d.DraftID)
                .ToList();

            if (!drafts.Any())
                return NotFound();

            var ownerIds = drafts
                .Select(d => d.Pet.OwnerID)
                .Distinct()
                .ToList();

            var ownerPets = _context.Pets
                .Where(p => ownerIds.Contains(p.OwnerID))
                .Select(p => new Pet {
                    PetID = p.PetID,
                    Name = p.Name,
                    Type = p.Type,
                    OwnerID = p.OwnerID
                })
                .ToList();

            var owners = _context.Owners.ToList();
            var categories = _context.ServiceCategories.ToList();
            var subtypes = _context.ServiceSubtypes.ToList();

            var vm = new AppointmentDraftGroupVM {
                GroupDraftId = draftId,
                Drafts = drafts,
                Owners = owners,
                Categories = categories,
                Subtypes = subtypes,

                OwnerPets = ownerPets
            };

            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDraftGroup([FromBody] AppointmentDraftGroupVM form) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return Json(new { success = false, message = "Unauthorized access." });

            if (form == null || form.Drafts == null || !form.Drafts.Any())
                return Json(new { success = false, message = "No drafts submitted." });

            if (form.Drafts.Count > 3)
                return Json(new { success = false, message = "A draft group can only contain a maximum of 3 appointments." });

            if (!form.OwnerID.HasValue)
                return Json(new { success = false, message = "Owner is required." });


            var validationList = new List<(DateTime dt, int index)>();

            for (int i = 0; i < form.Drafts.Count; i++) {
                var d = form.Drafts[i];

                if (d.PetID == 0)
                    return Json(new { success = false, message = $"Item #{i + 1} missing pet." });

                if (d.CategoryID == null)
                    return Json(new { success = false, message = $"Item #{i + 1} missing category." });

                if (d.SubtypeID == null)
                    return Json(new { success = false, message = $"Item #{i + 1} missing subtype." });

                if (d.AppointmentDate == default)
                    return Json(new { success = false, message = $"Item #{i + 1} missing date." });

                if (string.IsNullOrWhiteSpace(d.AppointmentTime))
                    return Json(new { success = false, message = $"Item #{i + 1} missing time." });

                if (!TimeSpan.TryParse(d.AppointmentTime, out var ts))
                    return Json(new { success = false, message = $"Invalid time format for item #{i + 1}." });

                var dateOnly = d.AppointmentDate.Date;
                var combinedDT = dateOnly.Add(ts);

                validationList.Add((combinedDT, i));

                d.AppointmentDate = dateOnly;
            }
            foreach (var item in validationList) {
                if (_context.Appointments.Any(a => a.AppointmentDate == item.dt)) {
                    return Json(new {
                        success = false,
                        message = $"Time slot {item.dt:MMM dd, yyyy hh:mm tt} is already taken by an existing appointment (item #{item.index + 1})."
                    });
                }
            }

            var otherDraftTimes = _context.AppointmentDrafts
                .Where(d => d.GroupDraftId != form.GroupDraftId)
                .Select(d => d.AppointmentDate.Date.Add(TimeSpan.Parse(d.AppointmentTime)))
                .ToList();

            foreach (var item in validationList) {
                if (otherDraftTimes.Contains(item.dt)) {
                    return Json(new {
                        success = false,
                        message = $"Time slot {item.dt:MMM dd, yyyy hh:mm tt} is already used in another draft group."
                    });
                }
            }

            using var tx = await _context.Database.BeginTransactionAsync();
            try {
                var existingDrafts = _context.AppointmentDrafts
                    .Where(d => d.GroupDraftId == form.GroupDraftId)
                    .ToList();

                var incomingIds = form.Drafts
                    .Where(d => d.DraftID > 0)
                    .Select(d => d.DraftID)
                    .ToHashSet();

                var toDelete = existingDrafts.Where(x => !incomingIds.Contains(x.DraftID)).ToList();
                if (toDelete.Any())
                    _context.AppointmentDrafts.RemoveRange(toDelete);

                var savedList = new List<AppointmentDraft>();
                var userId = HttpContext.Session.GetInt32("UserID");
                var userName = HttpContext.Session.GetString("UserName") ?? "Unknown";

                foreach (var d in form.Drafts) {
                    var target = existingDrafts.FirstOrDefault(x => x.DraftID == d.DraftID);

                    if (target == null) {
                        target = new AppointmentDraft {
                            GroupDraftId = form.GroupDraftId,
                            CreatedAt = DateTime.Now
                        };
                        _context.AppointmentDrafts.Add(target);
                    }

                    target.OwnerID = form.OwnerID.Value;
                    target.UserID = userId ?? target.UserID;
                    target.PetID = d.PetID;
                    target.CategoryID = d.CategoryID;
                    target.SubtypeID = d.SubtypeID;
                    target.AppointmentDate = d.AppointmentDate;
                    target.AppointmentTime = d.AppointmentTime;
                    target.Notes = string.IsNullOrWhiteSpace(d.Notes) ? "No notes." : d.Notes;

                    savedList.Add(target);
                }

                await _context.SaveChangesAsync();

                _context.Notifications.Add(new Notification {
                    Message = $"Draft group {form.GroupDraftId} updated by {userName}. {savedList.Count} services.",
                    Type = "Appointment",
                    TargetRole = "Staff"
                });

                foreach (var sd in savedList) {
                    var dt = sd.AppointmentDate.Date.Add(TimeSpan.Parse(sd.AppointmentTime));
                    _context.SystemLogs.Add(new SystemLog {
                        ActionType = "Update",
                        Module = "AppointmentDraft",
                        Description = $"Upsert draft {sd.DraftID} (Group {sd.GroupDraftId}) for PetID {sd.PetID} at {dt:MMM dd, yyyy hh:mm tt}",
                        PerformedBy = userName,
                        Timestamp = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return Json(new {
                    success = true,
                    message = $"Draft saved successfully. ({savedList.Count} services)",
                    groupDraftId = form.GroupDraftId,
                    drafts = savedList.Select(d => new {
                        d.DraftID,
                        d.GroupDraftId,
                        d.OwnerID,
                        d.PetID,
                        d.CategoryID,
                        d.SubtypeID,
                        AppointmentDate = d.AppointmentDate.ToString("yyyy-MM-dd"),
                        AppointmentTime = d.AppointmentTime,
                        d.Notes
                    }).ToList()
                });
            } catch (Exception ex) {
                await tx.RollbackAsync();
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }



        [HttpGet]
        public JsonResult GetSubtypesByCategoryId(int categoryId) {
            var subtypes = _context.ServiceSubtypes
                .Where(s => s.CategoryID == categoryId)
                .Select(s => new {
                    subtypeID = s.SubtypeID,
                    serviceSubType = s.ServiceSubType
                })
                .ToList();

            return Json(subtypes);
        }


        [HttpGet]
        public IActionResult GetAvailableTimeSlots(DateTime date) {
            var slots = new List<string>();
            var start = new TimeSpan(9, 0, 0);
            var end = new TimeSpan(18, 0, 0);

            var takenAppointments = _context.Appointments
                .Where(a => a.AppointmentDate.Date == date.Date)
                .Select(a => a.AppointmentDate.TimeOfDay)
                .ToList();

            var takenDrafts = _context.AppointmentDrafts
                .Where(d => d.AppointmentDate.Date == date.Date)
                .Select(d => TimeSpan.Parse(d.AppointmentTime))
                .ToList();

            var allTaken = takenAppointments
                .Concat(takenDrafts)
                .Distinct()
                .ToList();

            for (var t = start; t <= end; t = t.Add(TimeSpan.FromMinutes(5))) {
                if (!allTaken.Contains(t))
                    slots.Add(DateTime.Today.Add(t).ToString("HH:mm"));
            }

            return Json(slots);
        }

        [HttpGet]
        public IActionResult GetAppointmentsJson() {
            var real = _context.Appointments
                .Include(a => a.Pet)
                .Include(a => a.ServiceCategory)
                .Include(a => a.ServiceSubtype)
                .ToList();

            var realJson = real.Select(a => new {
                id = a.AppointmentID.ToString(),
                title = $"{a.Pet?.Name} � {a.ServiceCategory?.ServiceType ?? "No Category"} - {a.ServiceSubtype?.ServiceSubType ?? ""}",
                start = a.AppointmentDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                allDay = false,
                classNames = new[] { a.Status?.ToLower() ?? "unknown" },
                extendedProps = new {
                    petName = a.Pet?.Name,
                    category = a.ServiceCategory?.ServiceType,
                    subtype = a.ServiceSubtype?.ServiceSubType,
                    status = a.Status,
                    notes = a.Notes,
                    groupID = a.GroupID.ToString(),
                    isDraft = false
                }
            });
            var drafts = _context.AppointmentDrafts
                .Include(a => a.Pet)
                .Include(a => a.ServiceCategory)
                .Include(a => a.ServiceSubtype)
                .ToList();

            var draftJson = drafts.Select(a => new {
                id = $"DRAFT-{a.DraftID}",
                title = $"{a.Pet?.Name ?? "No Pet"} � {a.ServiceCategory?.ServiceType ?? "Draft"} - {a.ServiceSubtype?.ServiceSubType ?? ""}",
                start = a.AppointmentDate.ToString("yyyy-MM-dd") + "T" + (a.AppointmentTime ?? "00:00"),
                allDay = false,
                classNames = new[] { "draft" },
                extendedProps = new {
                    petName = a.Pet?.Name,
                    category = a.ServiceCategory?.ServiceType,
                    subtype = a.ServiceSubtype?.ServiceSubType,
                    status = "Draft",
                    notes = a.Notes,
                    groupID = a.GroupDraftId,
                    isDraft = true
                }
            });

            var final = realJson.Concat(draftJson).ToList();

            return Json(final);
        }
        [HttpPost]
        public IActionResult DeleteDraftGroup([FromBody] JsonElement body) {
            string groupDraftId = body.GetProperty("groupDraftId").GetString();

            var drafts = _context.AppointmentDrafts
                .Where(x => x.GroupDraftId == groupDraftId)
                .ToList();

            if (!drafts.Any())
                return Json(new { success = false, message = "No drafts found for this group." });

            _context.AppointmentDrafts.RemoveRange(drafts);
            _context.SaveChanges();

            return Json(new { success = true });
        }
        [HttpPost]
        public IActionResult DeleteAllDraftGroups([FromBody] List<string> groupDraftIds) {
            if (groupDraftIds == null || !groupDraftIds.Any())
                return Json(new { success = false, message = "No draft IDs provided." });

            var drafts = _context.AppointmentDrafts
                .Where(d => groupDraftIds.Contains(d.GroupDraftId))
                .ToList();

            if (drafts.Count == 0)
                return Json(new { success = false, message = "No drafts found." });

            _context.AppointmentDrafts.RemoveRange(drafts);
            _context.SaveChanges();

            return Json(new { success = true, message = "All draft groups deleted." });
        }

        [HttpGet]
        public IActionResult Pets(string searchQuery = "", string typeFilter = "", string sortField = "", string sortOrder = "", int page = 1) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            int pageSize = 10;

            if (string.IsNullOrEmpty(sortField))
                sortField = "id";
            if (string.IsNullOrEmpty(sortOrder))
                sortOrder = "asc";
            var query = _context.Pets.Include(p => p.Owner).AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery)) {
                query = query.Where(p =>
                    p.Name.Contains(searchQuery) ||
                    p.Type.Contains(searchQuery) ||
                    p.Breed.Contains(searchQuery) ||
                    p.Owner.Name.Contains(searchQuery));
            }
            if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "All") {
                query = query.Where(p => p.Type == typeFilter);
            }

            query = (sortField, sortOrder) switch {
                ("id", "desc") => query.OrderByDescending(p => p.PetID),
                ("id", "asc") => query.OrderBy(p => p.PetID),

                ("name", "desc") => query.OrderByDescending(p => p.Name),
                ("name", "asc") => query.OrderBy(p => p.Name),

                ("date", "desc") => query.OrderByDescending(p => p.Birthdate),
                _ => query.OrderBy(p => p.Birthdate)
            };
            int totalPets = query.Count();
            int totalPages = (int)Math.Ceiling(totalPets / (double)pageSize);

            var pets = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var viewModel = new PetsListViewModel {
                Pets = pets,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchQuery = searchQuery,
                TypeFilter = typeFilter
            };
            ViewBag.SortField = sortField;
            ViewBag.SortOrder = sortOrder;
            return View(viewModel);
        }
        [HttpGet]
        public IActionResult AddPet() {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            ViewBag.Owners = _context.Owners
              .Include(o => o.User)
              .ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPet(string Name, string Type, string Breed, DateTime Birthdate, int OwnerID, IFormFile? Photo) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return Json(new { success = false, message = "Unauthorized access." });

            if (string.IsNullOrWhiteSpace(Name) ||
                string.IsNullOrWhiteSpace(Type) ||
                string.IsNullOrWhiteSpace(Breed) ||
                Birthdate == default ||
                OwnerID == 0) {
                return Json(new { success = false, message = "All fields are required." });
            }

            try {
                string? photoPath = null;

                if (Photo != null && Photo.Length > 0) {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "pets");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var fileName = $"{Guid.NewGuid()}.jpg";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new MemoryStream()) {
                        await Photo.CopyToAsync(stream);
                        stream.Seek(0, SeekOrigin.Begin);

                        using (var original = System.Drawing.Image.FromStream(stream)) {
                            int side = Math.Min(original.Width, original.Height);
                            var cropRect = new System.Drawing.Rectangle(
                                (original.Width - side) / 2,
                                (original.Height - side) / 2,
                                side,
                                side);

                            using (var cropped = new System.Drawing.Bitmap(500, 500)) {
                                using (var g = System.Drawing.Graphics.FromImage(cropped)) {
                                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                                    g.DrawImage(original,
                                        new System.Drawing.Rectangle(0, 0, 500, 500),
                                        cropRect,
                                        System.Drawing.GraphicsUnit.Pixel);
                                }

                                cropped.Save(filePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                            }
                        }
                    }

                    photoPath = $"/uploads/pets/{fileName}";
                }

                var pet = new Pet {
                    OwnerID = OwnerID,
                    Name = Name,
                    Type = Type,
                    Breed = string.IsNullOrWhiteSpace(Breed) ? "N/A" : Breed,
                    Birthdate = Birthdate,
                    PhotoPath = photoPath
                };

                _context.Pets.Add(pet);
                await _context.SaveChangesAsync();

                _context.Notifications.Add(new Notification {
                    Message = $"A new pet '{pet.Name}' has been added by Owner ID:{pet.OwnerID}.",
                    Type = "Pet",
                    RedirectUrl = $"/{{role}}/ViewPet/{pet.PetID}",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    TargetRole = "Staff"
                });

                _context.SystemLogs.Add(new SystemLog {
                    ActionType = "Create",
                    Module = "Pet",
                    Description = $"Added a Pet: {pet.Name} (OwnerID: {pet.OwnerID})",
                    PerformedBy = HttpContext.Session.GetString("UserName") ?? "Unknown",
                    Timestamp = DateTime.Now
                });

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Pet added successfully!" });
            } catch (Exception ex) {
                Console.WriteLine(ex);
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }


        [HttpGet]
        public IActionResult EditPet(int id) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            var pet = _context.Pets.Include(p => p.Owner).FirstOrDefault(p => p.PetID == id);
            if (pet == null)
                return NotFound();

            ViewBag.Owners = _context.Owners
              .Include(o => o.User)
              .ToList();

            return View(pet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPet(int id, string Name, string Type, string Breed, DateTime Birthdate, int OwnerID, IFormFile? Photo) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return Json(new { success = false, message = "Unauthorized access." });

            var pet = _context.Pets.FirstOrDefault(p => p.PetID == id);
            if (pet == null)
                return Json(new { success = false, message = "Pet not found." });

            if (string.IsNullOrWhiteSpace(Name) ||
                string.IsNullOrWhiteSpace(Type) ||
                string.IsNullOrWhiteSpace(Breed) ||
                Birthdate == default ||
                OwnerID == 0) {
                return Json(new { success = false, message = "All fields are required." });
            }

            try {
                pet.Name = Name;
                pet.Type = Type;
                pet.Breed = Breed;
                pet.Birthdate = Birthdate;
                pet.OwnerID = OwnerID;

                if (Photo != null && Photo.Length > 0) {
                    if (!string.IsNullOrEmpty(pet.PhotoPath)) {
                        var oldPath = Path.Combine("wwwroot", pet.PhotoPath.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }

                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "pets");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var fileName = $"{Guid.NewGuid()}.jpg";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new MemoryStream()) {
                        await Photo.CopyToAsync(stream);
                        stream.Seek(0, SeekOrigin.Begin);

                        using (var original = System.Drawing.Image.FromStream(stream)) {
                            int side = Math.Min(original.Width, original.Height);
                            var cropRect = new System.Drawing.Rectangle(
                                (original.Width - side) / 2,
                                (original.Height - side) / 2,
                                side,
                                side);

                            using (var cropped = new System.Drawing.Bitmap(500, 500)) {
                                using (var g = System.Drawing.Graphics.FromImage(cropped)) {
                                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                                    g.DrawImage(original,
                                        new System.Drawing.Rectangle(0, 0, 500, 500),
                                        cropRect,
                                        System.Drawing.GraphicsUnit.Pixel);
                                }

                                cropped.Save(filePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                            }
                        }
                    }

                    pet.PhotoPath = $"/uploads/pets/{fileName}";
                }

                _context.Pets.Update(pet);
                _context.SystemLogs.Add(new SystemLog {
                    ActionType = "Update",
                    Module = "Pet",
                    Description = $"Updated Pet: {pet.Name} (OwnerID: {pet.OwnerID})",
                    PerformedBy = HttpContext.Session.GetString("UserName") ?? "Unknown",
                    Timestamp = DateTime.Now
                });

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Pet updated successfully!" });
            } catch (Exception ex) {
                Console.WriteLine($"[EditPet ERROR] {ex}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }



        [HttpGet]
        public IActionResult GetBreeds(string type) {
            if (string.IsNullOrWhiteSpace(type))
                return Json(new List<string>());

            string filePath = Path.Combine(_hostEnvironment.ContentRootPath, "App_Data",
                type.ToLower() == "dog" ? "dogs_dataset.csv" : "cats_dataset.csv");

            if (!System.IO.File.Exists(filePath))
                return Json(new List<string>());

            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture)) {
                var breeds = csv.GetRecords<dynamic>()
                                .Select(r => r.Breed?.ToString())
                                .Where(b => !string.IsNullOrWhiteSpace(b))
                                .Distinct()
                                .OrderBy(b => b)
                                .ToList();
                return Json(breeds);
            }
        }


        [HttpGet]
        public IActionResult ViewPet(int id, int page = 1, string searchQuery = "", int? categoryFilter = null) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            var pageSize = 5;

            var pet = _context.Pets
                .Include(p => p.Owner)
                .FirstOrDefault(p => p.PetID == id);

            if (pet == null)
                return NotFound();

            var query = _context.Appointments
                .Include(a => a.ServiceCategory)
                .Include(a => a.ServiceSubtype)
                .Where(a => a.PetID == id);

            if (categoryFilter.HasValue)
                query = query.Where(a => a.CategoryID == categoryFilter.Value);

            if (!string.IsNullOrEmpty(searchQuery))
                query = query.Where(a => a.Notes.Contains(searchQuery));

            var total = query.Count();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            var appointments = query
                .OrderByDescending(a => a.AppointmentDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var vm = new PetDetailsViewModel {
                Pet = pet,
                Appointments = appointments,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchQuery = searchQuery,
                CategoryFilter = categoryFilter
            };

            ViewBag.Categories = _context.ServiceCategories.ToList();

            return View(vm);
        }

        [HttpGet]
        public IActionResult PetCard(int id, int page = 1) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            var pet = _context.Pets
                .Include(p => p.Appointments)
                .ThenInclude(a => a.ServiceCategory)
                .Include(p => p.Appointments)
                .ThenInclude(a => a.ServiceSubtype)
                .Include(p => p.Owner)
                .FirstOrDefault(p => p.PetID == id);

            if (pet == null)
                return RedirectToAction("Pets");

            var completed = pet.Appointments
              .Where(a => a.Status == "Completed")
                .OrderByDescending(a => a.AppointmentDate)
                .ToList();

            int pageSize = 12;
            int totalPages = (int)Math.Ceiling((double)completed.Count / pageSize);

            var pageData = completed
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            int ageMonths = (int)Math.Floor((DateTime.Now - pet.Birthdate).TotalDays / 30.4375);

            return View("PetCard", new PetCardVM {
                Pet = pet,
                Records = completed,
                PageData = pageData,
                CurrentPage = page,
                TotalPages = totalPages,
                AgeInMonths = ageMonths
            });
        }

        [HttpGet]
        public IActionResult DownloadPetCardPdf(int id) {
            var pet = _context.Pets
                .Include(p => p.Owner)
                .Include(p => p.Appointments).ThenInclude(a => a.ServiceCategory)
                .Include(p => p.Appointments).ThenInclude(a => a.ServiceSubtype)
                .FirstOrDefault(p => p.PetID == id);

            if (pet == null)
                return RedirectToAction("Pets");

            var records = pet.Appointments
                .Where(a => a.Status == "Completed" &&
                       (a.ServiceCategory.ServiceType == "Vaccination" ||
                        a.ServiceCategory.ServiceType == "Deworming & Preventives"))
                .OrderBy(a => a.AppointmentDate)
                .ToList();

            string photoPath = System.IO.File.Exists($"wwwroot{pet.PhotoPath}")
                ? $"wwwroot{pet.PhotoPath}"
                : "wwwroot/uploads/profiles/pet.png";

            byte[] pdfBytes = QuestPDF.Fluent.Document.Create(container => {
                container.Page(page => {
                    page.Size(PageSizes.A5);
                    page.Margin(15);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col => {
                        col.Item()
                           .AlignCenter()
                           .Width(80)
                           .Image("wwwroot/images/happypawslogo.jpg")
                           .FitWidth();
                        col.Item().AlignCenter().Text("Happy Paws Veterinary Clinic").SemiBold().FontSize(14);
                        col.Item().AlignCenter().Text("Pet Health Card").FontSize(10);
                        col.Item().PaddingBottom(5).LineHorizontal(1);
                    });

                    page.Content().Column(col => {
                        col.Item().Row(row => {
                            row.RelativeColumn().Column(info => {
                                info.Item().Text($"Name: {pet.Name}");
                                info.Item().Text($"Species: {pet.Type}");
                                info.Item().Text($"Breed: {pet.Breed}");
                                info.Item().Text($"Birthdate: {pet.Birthdate:MMM dd, yyyy}");
                                info.Item().Text($"Pet ID: {pet.PetID:D6}");

                                info.Item().Text("");
                                info.Item().Text("Owner Information").SemiBold();

                                info.Item().Text($"Name: {pet.Owner.Name}");
                                info.Item().Text($"Contact: {pet.Owner.Phone}");
                                info.Item().Text($"Email: {pet.Owner.Email}");
                            });

                            row.ConstantColumn(90)
                               .Border(1)
                               .Padding(2)
                               .AlignCenter()
                               .Image(photoPath)
                               .FitArea();
                        });

                        col.Item().PaddingTop(6).LineHorizontal(1);

                        col.Item().PaddingTop(6).Table(table => {
                            table.ColumnsDefinition(cols => {
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(0.8f);
                                cols.RelativeColumn(1.2f);
                                cols.RelativeColumn(1.2f);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(1);
                            });

                            table.Header(h => {
                                h.Cell().Text("Date").SemiBold();
                                h.Cell().Text("Wt.").SemiBold();
                                h.Cell().Text("Vaccination").SemiBold();
                                h.Cell().Text("Ecto & Endo").SemiBold();
                                h.Cell().Text("Veterinarian").SemiBold();
                                h.Cell().Text("Next").SemiBold();
                            });

                            foreach (var r in records) {
                                table.Cell().Text(r.AppointmentDate.ToString("MM/dd/yyyy"));
                                table.Cell().Text(r.Notes?.Split("|")?[0] ?? "");

                                table.Cell().Text(
                                    r.ServiceCategory?.ServiceType == "Vaccination"
                                        ? (string.IsNullOrEmpty(r.ServiceSubtype?.ServiceSubType) ? "Not Availed" : r.ServiceSubtype?.ServiceSubType)
                                        : ""
                                );

                                table.Cell().Text(
                                    r.ServiceCategory?.ServiceType == "Deworming & Preventives"
                                        ? (string.IsNullOrEmpty(r.ServiceSubtype?.ServiceSubType) ? "Not Availed" : r.ServiceSubtype?.ServiceSubType)
                                        : ""
                                );

                                table.Cell().Text(r.AdministeredBy ?? "");
                                table.Cell().Text(r.DueDate?.ToString("MM/dd/yyyy") ?? "");
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text($"Generated {DateTime.Now:MM/dd/yyyy hh:mm tt}");
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"PetCard_{pet.Name}_{DateTime.Now:yyyyMMdd}.pdf");
        }



        [HttpGet]
        public IActionResult ViewOwner(int id, int page = 1, string searchQuery = "", string typeFilter = "") {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            int pageSize = 5;

            var owner = _context.Owners
                .Include(o => o.User)
                .Include(o => o.Pets)
                .FirstOrDefault(o => o.OwnerID == id);

            if (owner == null)
                return NotFound();

            var query = owner.Pets.AsQueryable();

            if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "All")
                query = query.Where(p => p.Type == typeFilter);

            if (!string.IsNullOrEmpty(searchQuery))
                query = query.Where(p => p.Name.ToLower().Contains(searchQuery.ToLower()));

            var total = query.Count();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            var pets = query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var vm = new OwnerDetailsViewModel {
                Owner = owner,
                Pets = pets,
                SearchQuery = searchQuery,
                TypeFilter = typeFilter,
                CurrentPage = page,
                TotalPages = totalPages
            };

            return View(vm);
        }



        [HttpGet]
        public IActionResult Owners(string searchQuery = "", string sortField = "", string sortOrder = "", string statusFilter = "", int page = 1) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            int pageSize = 10;

            if (string.IsNullOrEmpty(sortField))
                sortField = "id";
            if (string.IsNullOrEmpty(sortOrder))
                sortOrder = "asc";

            var query = _context.Owners.Include(o => o.User).AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery)) {
                query = query.Where(o =>
                    o.Name.Contains(searchQuery) ||
                    o.Email.Contains(searchQuery));
            }
            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All") {
                query = query.Where(o => o.User.Status == statusFilter);
            }
            query = (sortField, sortOrder) switch {
                ("id", "desc") => query.OrderByDescending(o => o.OwnerID),
                ("id", "asc") => query.OrderBy(o => o.OwnerID),

                ("name", "desc") => query.OrderByDescending(o => o.Name),
                ("name", "asc") => query.OrderBy(o => o.Name),

                ("email", "desc") => query.OrderByDescending(o => o.Email),
                ("email", "asc") => query.OrderBy(o => o.Email),

                ("phone", "desc") => query.OrderByDescending(o => o.Phone),
                ("phone", "asc") => query.OrderBy(o => o.Phone),

                _ => query.OrderBy(o => o.OwnerID)
            };

            int totalOwners = query.Count();
            int totalPages = (int)Math.Ceiling(totalOwners / (double)pageSize);

            var owners = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var viewModel = new OwnerListViewModel {
                Owners = owners,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchQuery = searchQuery,
                StatusFilter = statusFilter
            };

            ViewBag.SortField = sortField;
            ViewBag.SortOrder = sortOrder;

            return View(viewModel);
        }


        [HttpGet]
        public IActionResult EditOwner(int id) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            var owner = _context.Owners.Include(o => o.User).FirstOrDefault(o => o.OwnerID == id);
            if (owner == null)
                return NotFound();

            return View(owner);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditOwner(Owner model) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return Json(new { success = false, message = "Unauthorized access." });

            var owner = _context.Owners.FirstOrDefault(o => o.OwnerID == model.OwnerID);
            if (owner == null)
                return Json(new { success = false, message = "Owner not found." });

            try {
                owner.Name = model.Name;
                owner.Email = model.Email;
                owner.Phone = model.Phone;

                _context.Update(owner);
                _context.SaveChanges();
                _context.Notifications.Add(new Notification {
                    Message = $"Owner '{owner.Name}' has been updated.",
                    Type = "Owner",
                    RedirectUrl = $"/{{role}}/ViewOwner/{owner.OwnerID}",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    TargetRole = "Staff"
                });

                _context.SystemLogs.Add(new SystemLog {
                    ActionType = "Update",
                    Module = "Owner",
                    Description = $"Updated Owner: {owner.Name}",
                    PerformedBy = HttpContext.Session.GetString("UserName") ?? "Unknown",
                    Timestamp = DateTime.Now
                });

                _context.SaveChanges();

                return Json(new { success = true, message = "Owner updated successfully!" });
            } catch (Exception ex) {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Logs(string typeFilter = "", string moduleFilter = "", string searchQuery = "", string sortField = "", string sortOrder = "", int page = 1) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            const int pageSize = 10;
            var logsQuery = _context.SystemLogs.AsQueryable();

            if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "All")
                logsQuery = logsQuery.Where(l => l.ActionType == typeFilter);

            if (!string.IsNullOrEmpty(moduleFilter) && moduleFilter != "All")
                logsQuery = logsQuery.Where(l => l.Module == moduleFilter);

            if (!string.IsNullOrEmpty(searchQuery))
                logsQuery = logsQuery.Where(l =>
                    l.Description.Contains(searchQuery) ||
                    l.PerformedBy.Contains(searchQuery) ||
                    l.ActionType.Contains(searchQuery) ||
                    l.Module.Contains(searchQuery));

            if (string.IsNullOrEmpty(sortField)) sortField = "timestamp";
            if (string.IsNullOrEmpty(sortOrder)) sortOrder = "desc";

            logsQuery = (sortField, sortOrder) switch {
                ("action", "asc") => logsQuery.OrderBy(l => l.ActionType),
                ("action", "desc") => logsQuery.OrderByDescending(l => l.ActionType),

                ("module", "asc") => logsQuery.OrderBy(l => l.Module),
                ("module", "desc") => logsQuery.OrderByDescending(l => l.Module),

                ("performedby", "asc") => logsQuery.OrderBy(l => l.PerformedBy),
                ("performedby", "desc") => logsQuery.OrderByDescending(l => l.PerformedBy),

                ("timestamp", "asc") => logsQuery.OrderBy(l => l.Timestamp),
                ("timestamp", "desc") => logsQuery.OrderByDescending(l => l.Timestamp),

                _ => logsQuery.OrderByDescending(l => l.Timestamp)
            };

            int totalLogs = await logsQuery.CountAsync();
            var logs = await logsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new LogListViewModel {
                Logs = logs,
                TypeFilter = typeFilter,
                ModuleFilter = moduleFilter,
                SearchQuery = searchQuery,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalLogs / (double)pageSize)
            };

            ViewBag.SortField = sortField;
            ViewBag.SortOrder = sortOrder;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ClearLogs() {
            try {
                var allLogs = _context.SystemLogs.ToList();
                if (allLogs.Any()) {
                    _context.SystemLogs.RemoveRange(allLogs);
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "All logs have been cleared successfully." });
            } catch (Exception ex) {
                return Json(new { success = false, message = $"Error clearing logs: {ex.Message}" });
            }
        }
        [HttpGet]
        public IActionResult Predictive_Analytics() {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            var model = new DashboardViewModel {
                TotalCategory = _context.ServiceCategories.Count(),
                TotalType = _context.ServiceSubtypes.Count(),
                TotalAppointments = _context.Appointments.Count(),
            };

            // load CSV history
            var path = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "MonthlySummary_HappyPaws.csv");
            List<ServiceDemandData> csvHistory;
            using (var reader = new StreamReader(path))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture))) {
                csv.Context.RegisterClassMap<ServiceDemandDataMap>();
                csvHistory = csv.GetRecords<ServiceDemandData>().ToList();
            }

            // Merge db data
            var dbSummary = _context.Appointments
                .Where(a => a.Status == "Completed" && a.CategoryID != null)
                .GroupBy(a => new { a.AppointmentDate.Year, a.AppointmentDate.Month, Category = a.ServiceCategory.ServiceType })
                .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Category, Count = g.Count() })
                .ToList();

            foreach (var group in dbSummary) {
                var existing = csvHistory.FirstOrDefault(x => x.Year == group.Year && x.Month == group.Month);
                if (existing == null) {
                    existing = new ServiceDemandData { Year = group.Year, Month = group.Month };
                    csvHistory.Add(existing);
                }

                switch (group.Category) {
                    case "Confinement / Hospitalization": existing.Confinement += group.Count; break;
                    case "Deworming & Preventives": existing.Deworming += group.Count; break;
                    case "End of Life Care": existing.EndOfLifeCare += group.Count; break;
                    case "Grooming & Wellness": existing.Grooming += group.Count; break;
                    case "Medication & Treatment": existing.Medication += group.Count; break;
                    case "Professional Fee / Consultation": existing.Consultation += group.Count; break;
                    case "Specialty Tests / Rare Cases": existing.SpecialtyTests += group.Count; break;
                    case "Surgery": existing.Surgery += group.Count; break;
                    case "Vaccination": existing.Vaccination += group.Count; break;
                    case "Diagnostics & Laboratory Tests": existing.Diagnostics += group.Count; break;
                }
            }

            csvHistory = csvHistory.OrderBy(x => x.Year).ThenBy(x => x.Month).ToList();

            model.Years = csvHistory.Select(h => h.Year).Distinct().OrderBy(y => y).ToList();
            var currentYear = DateTime.Now.Year;
            if (!model.Years.Contains(currentYear)) {
                model.Years.Add(currentYear);
                model.Years = model.Years.OrderBy(y => y).ToList();
            }

            // use forecastzip
            var modelPath = Path.Combine(Directory.GetCurrentDirectory(), "ExportedModels");
            var predictor = new ForecastServiceWithZip(modelPath);

            var last = csvHistory.Last();
            int nextMonth = last.Month + 1;
            int nextYear = last.Year;
            if (nextMonth > 12) {
                nextMonth = 1;
                nextYear++;
            }

            var input = new ServiceDemandData {
                Year = nextYear,
                Month = nextMonth,
                Month_sin = (float)Math.Sin(2 * Math.PI * nextMonth / 12.0),
                Month_cos = (float)Math.Cos(2 * Math.PI * nextMonth / 12.0),
                IsPeakSeason = (nextMonth == 4 || nextMonth == 5 || nextMonth == 12) ? 1 : 0,
                IsSlowSeason = (nextMonth >= 7 && nextMonth <= 10) ? 1 : 0,
                IsHoliday = (nextMonth == 12) ? 1 : 0,
                Lag1_Total = last.Rolling3_Total,
                Lag2_Total = last.Lag2_Total,
                Lag3_Total = last.Lag3_Total,
                Rolling3_Total = last.Rolling3_Total,
                Rolling6_Total = last.Rolling6_Total
            };

            // for "all" string and years
            float prediction = predictor.Predict(input, "All");

            model.PredictedNextValue = prediction;
            model.PredictedMonthLabel = $"{nextMonth}/{nextYear}";

            return View(model);
        }
        [HttpGet]
        public JsonResult GetForecastData(string service, string year) {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "MonthlySummary_HappyPaws.csv");
            List<ServiceDemandData> csvHistory;
            using (var reader = new StreamReader(path))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture))) {
                csv.Context.RegisterClassMap<ServiceDemandDataMap>();
                csvHistory = csv.GetRecords<ServiceDemandData>().ToList();
            }

            // merged data csv and db
            var dbSummary = _context.Appointments
                .Where(a => a.Status == "Completed" && a.CategoryID != null)
                .GroupBy(a => new { a.AppointmentDate.Year, a.AppointmentDate.Month, Category = a.ServiceCategory.ServiceType })
                .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Category, Count = g.Count() })
                .ToList();

            foreach (var group in dbSummary) {
                var existing = csvHistory.FirstOrDefault(x => x.Year == group.Year && x.Month == group.Month);
                if (existing == null) {
                    existing = new ServiceDemandData { Year = group.Year, Month = group.Month };
                    csvHistory.Add(existing);
                }

                switch (group.Category) {
                    case "Confinement / Hospitalization": existing.Confinement += group.Count; break;
                    case "Deworming & Preventives": existing.Deworming += group.Count; break;
                    case "End of Life Care": existing.EndOfLifeCare += group.Count; break;
                    case "Grooming & Wellness": existing.Grooming += group.Count; break;
                    case "Medication & Treatment": existing.Medication += group.Count; break;
                    case "Professional Fee / Consultation": existing.Consultation += group.Count; break;
                    case "Specialty Tests / Rare Cases": existing.SpecialtyTests += group.Count; break;
                    case "Surgery": existing.Surgery += group.Count; break;
                    case "Vaccination": existing.Vaccination += group.Count; break;
                    case "Diagnostics & Laboratory Tests": existing.Diagnostics += group.Count; break;
                }
            }

            csvHistory = csvHistory.OrderBy(x => x.Year).ThenBy(x => x.Month).ToList();

            var modelPath = Path.Combine(Directory.GetCurrentDirectory(), "ExportedModels");
            var predictor = new ForecastServiceWithZip(modelPath);

            var result = predictor.GetForecastData(csvHistory, service, year);

            return Json(new {
                months = result.Months,
                counts = result.ActualCounts,
                predictedCounts = result.PredictedCounts,
                futureForecasts = result.FutureForecasts,
                nextMonth = result.NextMonth,
                nextForecastValue = result.NextForecastValue,
                severity = result.Severity,
                forecastMonths = result.ForecastMonths,
                globalFutureForecasts = result.GlobalFutureForecasts,
                globalSeverities = result.GlobalSeverities,
                nextMonthServiceRanking = result.NextMonthServiceRanking
            });
        }
        //     [HttpPost]
        //   [ValidateAntiForgeryToken]
        //   public IActionResult RetrainForecast()
        //   {
        //       return RedirectToAction("Dashboard");
        //   }
        public JsonResult GetNotifications() {
            var currentUserId = HttpContext.Session.GetInt32("UserID");
            var userRole = HttpContext.Session.GetString("UserRole") ?? "Staff";

            var notificationsRaw = _context.Notifications
                .Where(n => (n.TargetRole == "Staff" || n.TargetRole == null) && (n.TargetUserId == null || n.TargetUserId == currentUserId))
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            var notifications = notificationsRaw.Select(n => new {
                notificationID = n.NotificationID,
                message = n.Message,
                type = n.Type,
                createdAt = n.CreatedAt.ToString("MMM dd, yyyy hh:mm tt"),
                isRead = n.IsRead,
                redirectUrl = n.RedirectUrl?.Replace("{role}", userRole).Replace("/Admin/", $"/{userRole}/").Replace("/Staff/", $"/{userRole}/")
            }).ToList();

            var unreadCount = notifications.Count(n => !n.isRead);

            return Json(new {
                notifications = notifications.Take(10),
                totalUnread = unreadCount
            });
        }

        [HttpPost]
        public IActionResult MarkAllNotificationsRead() {
            try {
                var unread = _context.Notifications.Where(n => !n.IsRead).ToList();

                if (unread.Any()) {
                    foreach (var n in unread)
                        n.IsRead = true;

                    _context.SaveChanges();

                    return Json(new {
                        success = true,
                        message = "All notifications have been marked as read."
                    });
                } else {
                    return Json(new {
                        success = false,
                        message = "There are no unread notifications to mark."
                    });
                }
            } catch (Exception ex) {
                return Json(new {
                    success = false,
                    message = "An error occurred while marking notifications as read: " + ex.Message
                });
            }
        }
        [HttpPost]
        public IActionResult MarkNotificationRead(int id) {
            var notif = _context.Notifications.FirstOrDefault(n => n.NotificationID == id);
            if (notif == null) return NotFound();

            notif.IsRead = true;
            _context.SaveChanges();

            return Ok();
        }


        [HttpGet]
        public IActionResult AdminNotification(string typeFilter = "All", string statusFilter = "All", string searchQuery = "", int page = 1) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            const int pageSize = 10;
            var currentUserId = HttpContext.Session.GetInt32("UserID");

            var query = _context.Notifications.AsQueryable();

            query = query.Where(n => (n.TargetRole == "Staff" || n.TargetRole == null) && (n.TargetUserId == null || n.TargetUserId == currentUserId));

            if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "All") {
                query = query.Where(n => n.Type == typeFilter);
            }

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All") {
                query = statusFilter == "Read" ? query.Where(n => n.IsRead) : query.Where(n => !n.IsRead);
            }

            if (!string.IsNullOrEmpty(searchQuery)) {
                query = query.Where(n => n.Message.Contains(searchQuery));
            }

            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var notificationsRaw = query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var notifications = notificationsRaw.Select(n => new NotificationViewModel {
                NotificationID = n.NotificationID,
                Message = n.Message,
                Type = n.Type,
                CreatedAt = n.CreatedAt,
                IsRead = n.IsRead,
                RedirectUrl = n.RedirectUrl?.Replace("{role}", "Staff").Replace("/Admin/", "/Staff/")
            }).ToList();

            var model = new NotificationListViewModel {
                Notifications = notifications,
                TypeFilter = typeFilter,
                StatusFilter = statusFilter,
                SearchQuery = searchQuery,
                CurrentPage = page,
                TotalPages = totalPages
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult ServiceCategory(string searchQuery = "", string sortOrder = "", string sortField = "", int page = 1) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            int pageSize = 10;
            if (string.IsNullOrEmpty(sortField))
                sortField = "id";
            if (string.IsNullOrEmpty(sortOrder))
                sortOrder = "asc";
            var query = _context.ServiceCategories.AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery)) {
                query = query.Where(c =>
                    c.ServiceType.Contains(searchQuery) ||
                    c.Description.Contains(searchQuery));
            }
            query = (sortField, sortOrder) switch {
                ("id", "desc") => query.OrderByDescending(c => c.CategoryID),
                ("id", "asc") => query.OrderBy(c => c.CategoryID),

                ("name", "desc") => query.OrderByDescending(c => c.ServiceType),
                ("name", "asc") => query.OrderBy(c => c.ServiceType),

                ("desc", "desc") => query.OrderByDescending(c => c.Description),
                ("desc", "asc") => query.OrderBy(c => c.Description),

                _ => query.OrderBy(c => c.CategoryID)
            };

            int totalCategories = query.Count();
            int totalPages = (int)Math.Ceiling(totalCategories / (double)pageSize);

            var categories = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var viewModel = new ServiceCategoryListViewModel {
                ServiceCategories = categories,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchQuery = searchQuery
            };

            ViewBag.SortOrder = sortOrder;
            ViewBag.SortField = sortField;
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult ServiceType(string searchQuery = "", string sortOrder = "", string sortField = "", int page = 1, int? categoryFilter = null) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            int pageSize = 10;
            if (string.IsNullOrEmpty(sortField))
                sortField = "id";
            if (string.IsNullOrEmpty(sortOrder))
                sortOrder = "asc";
            var query = _context.ServiceSubtypes
                .Include(st => st.ServiceCategory)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery)) {
                query = query.Where(st =>
                    st.ServiceSubType.Contains(searchQuery) ||
                    st.ServiceCategory.ServiceType.Contains(searchQuery));
            }
            query = (sortField, sortOrder) switch {
                ("id", "desc") => query.OrderByDescending(st => st.SubtypeID),
                ("id", "asc") => query.OrderBy(st => st.SubtypeID),

                ("name", "desc") => query.OrderByDescending(st => st.ServiceSubType),
                ("name", "asc") => query.OrderBy(st => st.ServiceSubType),

                ("desc", "desc") => query.OrderByDescending(st => st.Description),
                ("desc", "asc") => query.OrderBy(st => st.Description),

                _ => query.OrderBy(st => st.SubtypeID)
            };

            if (categoryFilter.HasValue && categoryFilter > 0) {
                query = query.Where(st => st.CategoryID == categoryFilter.Value);
            }

            int totalSubtypes = query.Count();
            int totalPages = (int)Math.Ceiling(totalSubtypes / (double)pageSize);

            var subtypes = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var viewModel = new ServiceTypeListViewModel {
                ServiceSubtypes = subtypes,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchQuery = searchQuery,
                CategoryFilter = categoryFilter
            };

            ViewBag.Categories = _context.ServiceCategories.ToList();
            ViewBag.SortOrder = sortOrder;
            ViewBag.SortField = sortField;
            return View(viewModel);
        }
        [HttpGet]
        public IActionResult AddServiceCategory() {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddServiceCategory(ServiceCategory model) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return Json(new { success = false, message = "Unauthorized access." });

            if (string.IsNullOrWhiteSpace(model.ServiceType))
                return Json(new { success = false, message = "Service type is required." });

            try {
                _context.ServiceCategories.Add(model);

                _context.Notifications.Add(new Notification {
                    Message = $"A new service category '{model.ServiceType}' has been added.",
                    Type = "Service Category",
                    RedirectUrl = $"/{{role}}/EditServiceCategory/{model.CategoryID}",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    TargetRole = "Staff"
                });
                _context.SystemLogs.Add(new SystemLog {
                    ActionType = "Create",
                    Module = "Service",
                    Description = $"Added a Service Category: {model.ServiceType}",
                    PerformedBy = HttpContext.Session.GetString("UserName") ?? "Unknown",
                    Timestamp = DateTime.Now
                });
                _context.SaveChanges();
                return Json(new { success = true, message = "Service category added successfully!" });
            } catch (Exception ex) {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult EditServiceCategory(int id) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            var category = _context.ServiceCategories.Find(id);
            if (category == null)
                return NotFound();

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditServiceCategory(ServiceCategory model) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return Json(new { success = false, message = "Unauthorized access." });

            var category = _context.ServiceCategories.Find(model.CategoryID);
            if (category == null)
                return Json(new { success = false, message = "Service category not found." });

            try {
                category.ServiceType = model.ServiceType;
                category.Description = model.Description;
                _context.Update(category);

                _context.Notifications.Add(new Notification {
                    Message = $"Service category '{model.ServiceType}' has been updated.",
                    Type = "Service Category",
                    RedirectUrl = $"/{{role}}/EditServiceCategory/{model.CategoryID}",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    TargetRole = "Staff"
                });
                _context.SystemLogs.Add(new SystemLog {
                    ActionType = "Update",
                    Module = "Service",
                    Description = $"Updated a Service Category: {model.ServiceType}",
                    PerformedBy = HttpContext.Session.GetString("UserName") ?? "Unknown",
                    Timestamp = DateTime.Now
                });
                _context.SaveChanges();
                return Json(new { success = true, message = "Service category updated successfully!" });
            } catch (Exception ex) {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
        [HttpGet]
        public IActionResult AddServiceSubtype() {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            ViewBag.ServiceCategories = _context.ServiceCategories
                .Select(c => new {
                    c.CategoryID,
                    c.ServiceType
                }).ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddServiceSubtype(ServiceSubtype model) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return Json(new { success = false, message = "Unauthorized access." });

            if (string.IsNullOrWhiteSpace(model.ServiceSubType) || model.CategoryID == 0)
                return Json(new { success = false, message = "Please fill all required fields." });

            try {
                _context.ServiceSubtypes.Add(model);
                _context.Notifications.Add(new Notification {
                    Message = $"New service subtype '{model.ServiceSubType}' has been added under Category ID {model.CategoryID}.",
                    Type = "Service Subtype",
                    RedirectUrl = $"/{{role}}/EditServiceSubtype/{model.SubtypeID}",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    TargetRole = "Staff"
                });
                _context.SystemLogs.Add(new SystemLog {
                    ActionType = "Create",
                    Module = "Service",
                    Description = $"Added a Service Type: {model.ServiceSubType}",
                    PerformedBy = HttpContext.Session.GetString("UserName") ?? "Unknown",
                    Timestamp = DateTime.Now
                });
                _context.SaveChanges();
                return Json(new { success = true, message = "Service Subtype added successfully!" });
            } catch (Exception ex) {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
        [HttpGet]
        public IActionResult EditServiceSubtype(int id) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            var subtype = _context.ServiceSubtypes.Find(id);
            if (subtype == null)
                return NotFound();

            ViewBag.ServiceCategories = _context.ServiceCategories
                .Select(c => new {
                    c.CategoryID,
                    c.ServiceType
                }).ToList();

            return View(subtype);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditServiceSubtype(ServiceSubtype model) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return Json(new { success = false, message = "Unauthorized access." });

            var existingSubtype = _context.ServiceSubtypes.Find(model.SubtypeID);
            if (existingSubtype == null)
                return Json(new { success = false, message = "Service subtype not found." });

            try {
                existingSubtype.ServiceSubType = model.ServiceSubType;
                existingSubtype.Description = model.Description;
                existingSubtype.CategoryID = model.CategoryID;

                _context.Update(existingSubtype);

                _context.Notifications.Add(new Notification {
                    Message = $"Service subtype '{model.ServiceSubType}' has been updated.",
                    Type = "Service Subtype",
                    RedirectUrl = $"/{{role}}/EditServiceSubtype/{model.SubtypeID}",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    TargetRole = "Staff"
                });
                _context.SystemLogs.Add(new SystemLog {
                    ActionType = "Update",
                    Module = "Service",
                    Description = $"Updated Service Subtype: {model.ServiceSubType}",
                    PerformedBy = HttpContext.Session.GetString("UserName") ?? "Unknown",
                    Timestamp = DateTime.Now
                });
                _context.SaveChanges();

                return Json(new { success = true, message = "Service subtype updated successfully!" });
            } catch (Exception ex) {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
        [HttpGet]
        public JsonResult GetServiceCategories() {
            var categories = _context.ServiceCategories
                .Select(c => new { c.CategoryID, c.ServiceType })
                .ToList();
            return Json(categories);
        }
        [HttpGet]
        public JsonResult GetServiceTypes(int categoryId) {
            var types = _context.ServiceSubtypes
                .Where(t => t.CategoryID == categoryId)
                .Select(t => new { t.SubtypeID, t.ServiceSubType })
                .ToList();
            return Json(types);
        }
        public IActionResult Reports() {
            ViewBag.ServiceCategories = _context.ServiceCategories
                .Select(c => new {
                    c.CategoryID,
                    c.ServiceType
                })
                .ToList();

            ViewBag.ServiceSubtypes = _context.ServiceSubtypes
                .Select(s => new {
                    s.SubtypeID,
                    s.ServiceSubType,
                    s.CategoryID
                })
                .ToList();

            ViewBag.LogActionTypes = _context.SystemLogs
                .Select(l => l.ActionType)
                .Distinct()
                .ToList();

            ViewBag.LogModules = _context.SystemLogs
                .Select(l => l.Module)
                .Distinct()
                .ToList();

            ViewBag.NotificationActionTypes = _context.Notifications
                .Select(n => n.Type)
                .Distinct()
                .ToList();

            return View();
        }


        [HttpGet]
        public JsonResult GenerateReport(string type, DateTime? from, DateTime? to, string categoryFilter = "0", string typeFilter = "0", string statusFilter = "All", string dateSort = "desc", string readFilter = "All", string notifTypeFilter = "All",
        string logActionTypeFilter = "All", string logModuleFilter = "All") {
            var result = new List<object>();
            if (string.IsNullOrWhiteSpace(type)) return Json(result);

            var normalizedType = type.Trim().ToLowerInvariant();
            var normalizedDateSort = (dateSort ?? "desc").Trim().ToLowerInvariant();

            DateTime? toInclusive = null;
            if (to.HasValue)
                toInclusive = to.Value.Date.AddDays(1).AddTicks(-1);

            switch (normalizedType) {
                case "appointments":
                    var appts = _context.Appointments
                        .Include(a => a.Pet)
                        .Include(a => a.ServiceSubtype)
                            .ThenInclude(s => s.ServiceCategory)
                        .AsQueryable();

                    if (from.HasValue) appts = appts.Where(a => a.AppointmentDate >= from.Value);
                    if (toInclusive.HasValue) appts = appts.Where(a => a.AppointmentDate <= toInclusive.Value);

                    if (int.TryParse(categoryFilter, out int catId) && catId > 0)
                        appts = appts.Where(a => a.ServiceSubtype != null && a.ServiceSubtype.CategoryID == catId);

                    if (int.TryParse(typeFilter, out int typeId) && typeId > 0)
                        appts = appts.Where(a => a.ServiceSubtype != null && a.ServiceSubtype.SubtypeID == typeId);

                    if (!string.IsNullOrEmpty(statusFilter) && !string.Equals(statusFilter, "All", StringComparison.OrdinalIgnoreCase))
                        appts = appts.Where(a => a.Status == statusFilter);

                    appts = normalizedDateSort == "asc"
                        ? appts.OrderBy(a => a.AppointmentDate)
                        : appts.OrderByDescending(a => a.AppointmentDate);

                    result = appts.Select(a => new {
                        ID = a.AppointmentID,
                        Pet = a.Pet != null ? a.Pet.Name : "N/A",
                        Category = a.ServiceSubtype != null && a.ServiceSubtype.ServiceCategory != null
                            ? a.ServiceSubtype.ServiceCategory.ServiceType
                            : "N/A",
                        Service = a.ServiceSubtype != null ? a.ServiceSubtype.ServiceSubType : "N/A",
                        Status = a.Status,
                        Date = a.AppointmentDate.ToString("MMM dd, yyyy hh:mm tt")
                    }).ToList<object>();
                    break;

                case "notifications":
                    var notif = _context.Notifications
                        .AsQueryable()
                        .Where(n => (!from.HasValue || n.CreatedAt >= from.Value) &&
                                    (!toInclusive.HasValue || n.CreatedAt <= toInclusive.Value));

                    if (!string.IsNullOrEmpty(readFilter) && !string.Equals(readFilter, "All", StringComparison.OrdinalIgnoreCase)) {
                        if (string.Equals(readFilter, "Yes", StringComparison.OrdinalIgnoreCase))
                            notif = notif.Where(n => n.IsRead);
                        else if (string.Equals(readFilter, "No", StringComparison.OrdinalIgnoreCase))
                            notif = notif.Where(n => !n.IsRead);
                    }

                    if (!string.IsNullOrEmpty(notifTypeFilter) && !string.Equals(notifTypeFilter, "All", StringComparison.OrdinalIgnoreCase))
                        notif = notif.Where(n => n.Type == notifTypeFilter);

                    notif = normalizedDateSort == "asc" ? notif.OrderBy(n => n.CreatedAt) : notif.OrderByDescending(n => n.CreatedAt);

                    result = notif.Select(n => new {
                        ID = n.NotificationID,
                        Type = n.Type,
                        Message = n.Message,
                        IsRead = n.IsRead ? "Yes" : "No",
                        Created = n.CreatedAt.ToString("MMM dd, yyyy hh:mm tt")
                    }).ToList<object>();
                    break;

                case "logs":
                    var logs = _context.SystemLogs
                        .AsQueryable()
                        .Where(l => (!from.HasValue || l.Timestamp >= from.Value) &&
                                    (!toInclusive.HasValue || l.Timestamp <= toInclusive.Value));

                    if (!string.IsNullOrEmpty(logActionTypeFilter) && !string.Equals(logActionTypeFilter, "All", StringComparison.OrdinalIgnoreCase))
                        logs = logs.Where(l => l.ActionType == logActionTypeFilter);

                    if (!string.IsNullOrEmpty(logModuleFilter) && !string.Equals(logModuleFilter, "All", StringComparison.OrdinalIgnoreCase))
                        logs = logs.Where(l => l.Module == logModuleFilter);

                    logs = normalizedDateSort == "asc" ? logs.OrderBy(l => l.Timestamp) : logs.OrderByDescending(l => l.Timestamp);

                    result = logs.Select(l => new {
                        ID = l.LogID,
                        ActionType = l.ActionType,
                        Module = l.Module,
                        Description = l.Description,
                        PerformedBy = l.PerformedBy,
                        Date = l.Timestamp.ToString("MMM dd, yyyy hh:mm tt")
                    }).ToList<object>();
                    break;
            }

            return Json(result);
        }

        [HttpGet]
        public IActionResult DownloadReportPdf(
            string type,
            DateTime? from,
            DateTime? to,
            string categoryFilter = "0",
            string typeFilter = "0",
            string statusFilter = "All",
            string dateSort = "desc",
            string readFilter = "All",
            string notifTypeFilter = "All",
            string logActionTypeFilter = "All",
            string logModuleFilter = "All") {
            var json = GenerateReport(type, from, to, categoryFilter, typeFilter, statusFilter, dateSort, readFilter, notifTypeFilter, logActionTypeFilter, logModuleFilter);
            var list = json?.Value as IEnumerable<object> ?? Enumerable.Empty<object>();

            byte[] pdfBytes = QuestPDF.Fluent.Document.Create(container => {
                container.Page(page => {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(10));
                    page.Header()
                        .Text("Happy Paws Veterinary Clinic")
                        .SemiBold()
                        .FontSize(18)
                        .AlignCenter();

                    page.Content()
                        .PaddingVertical(10)
                        .Column(col => {
                            col.Item().Text($"Report Type: {type?.ToUpper() ?? "N/A"}").AlignCenter();
                            col.Item().Text($"Date Range: {from?.ToString("yyyy-MM-dd") ?? "All"} to {to?.ToString("yyyy-MM-dd") ?? "All"}").AlignCenter();
                            col.Item().PaddingTop(10);

                            if (list.Any()) {
                                var props = list.First().GetType().GetProperties();
                                col.Item().Table(table => {
                                    table.ColumnsDefinition(columns => {
                                        for (int i = 0; i < props.Length; i++)
                                            columns.RelativeColumn();
                                    });

                                    table.Header(header => {
                                        foreach (var prop in props)
                                            header.Cell().Text(prop.Name).SemiBold();
                                    });

                                    foreach (var item in list) {
                                        foreach (var prop in props) {
                                            var value = prop.GetValue(item)?.ToString() ?? "";
                                            table.Cell().Text(value);
                                        }
                                    }
                                });
                            } else {
                                col.Item().Text("No data available for this report.").AlignCenter().Italic();
                            }
                        });
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"{type}_report_{DateTime.Now:yyyyMMddHHmm}.pdf");
        }

        [HttpGet]
        public IActionResult DownloadReportCsv(
            string type,
            DateTime? from,
            DateTime? to,
            string categoryFilter = "0",
            string typeFilter = "0",
            string statusFilter = "All",
            string dateSort = "desc",
            string readFilter = "All",
            string notifTypeFilter = "All",
            string logActionTypeFilter = "All",
            string logModuleFilter = "All") {
            var json = GenerateReport(type, from, to, categoryFilter, typeFilter, statusFilter, dateSort, readFilter, notifTypeFilter, logActionTypeFilter, logModuleFilter);
            var list = json?.Value as IEnumerable<object> ?? Enumerable.Empty<object>();

            if (!list.Any())
                return Content("No data available for this report.");

            var sb = new StringBuilder();
            var props = list.First().GetType().GetProperties();

            sb.AppendLine(string.Join(",", props.Select(p => p.Name)));
            foreach (var item in list) {
                sb.AppendLine(string.Join(",", props.Select(p => (p.GetValue(item)?.ToString() ?? "").Replace(",", " "))));
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"{type}_report_{DateTime.Now:yyyyMMddHHmm}.csv");
        }

        [HttpGet]
        public IActionResult Profile() {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserID");

            if (string.IsNullOrEmpty(role) || userId == null)
                return RedirectToAction("Login", "Account");

            if (role != "Staff")
                return RedirectToAction("Login", "Account");

            var adminUser = _context.Users.FirstOrDefault(u => u.UserID == userId && u.Type == "Staff");
            if (adminUser == null)
                return NotFound();

            var msConnection = _context.MicrosoftAccountConnections
                .FirstOrDefault(c => c.UserID == userId);

            if (msConnection != null)
                HttpContext.Session.SetString("MicrosoftEmail", msConnection.MicrosoftEmail);
            else
                HttpContext.Session.Remove("MicrosoftEmail");

            ViewBag.ConnectedMicrosoftEmail = msConnection?.MicrosoftEmail;
            ViewBag.AccessToken = msConnection?.AccessToken;
            ViewBag.RefreshToken = msConnection?.RefreshToken;
            ViewBag.TokenExpiry = msConnection?.TokenExpiry;
            ViewBag.MicrosoftConnected = TempData["MicrosoftConnected"] as bool? ?? false;

            return View(adminUser);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUserDetails(
    int id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string CurrentPassword,
    string Password,
    string ConfirmPassword) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return Json(new { success = false, message = "Unauthorized access." });

            var user = _context.Users.FirstOrDefault(u => u.UserID == id);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
                return Json(new { success = false, message = "First name and last name are required." });

            if (!Regex.IsMatch(FirstName ?? "", @"^[a-zA-Z\s\\-]+$") || !Regex.IsMatch(LastName ?? "", @"^[a-zA-Z\s\\-]+$"))
                return Json(new { success = false, message = "Names must not contain special characters or numbers." });

            if (string.IsNullOrWhiteSpace(Phone) || !Regex.IsMatch(Phone, @"^\d{11}$"))
                return Json(new { success = false, message = "Phone number must be exactly 11 digits." });
            user.FirstName = FirstName.Trim();
            user.LastName = LastName.Trim();
            user.Phone = Phone?.Trim();

            if (!string.IsNullOrWhiteSpace(Password)) {
                if (string.IsNullOrWhiteSpace(CurrentPassword))
                    return Json(new { success = false, message = "Enter your current password to change it." });

                var hasher = new PasswordHasher<PurrVetUser>();
                var verify = hasher.VerifyHashedPassword(user, user.Password, CurrentPassword);
                if (verify == PasswordVerificationResult.Failed)
                    return Json(new { success = false, message = "Incorrect current password." });

                if (Password != ConfirmPassword)
                    return Json(new { success = false, message = "Passwords do not match." });

                if (Password.Length < 6)
                    return Json(new { success = false, message = "Password must be at least 6 characters." });

                user.Password = hasher.HashPassword(user, Password);
            }

            try {
                _context.Users.Update(user);
                _context.SystemLogs.Add(new SystemLog {
                    ActionType = "Update",
                    Module = "User",
                    Description = $"Updated profile: {user.FirstName} {user.LastName}",
                    PerformedBy = HttpContext.Session.GetString("UserName") ?? "Unknown",
                    Timestamp = DateTime.Now
                });
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Profile updated successfully!" });
            } catch (Exception ex) {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUserPhoto(int id, IFormFile? ProfileImage) {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return Json(new { success = false, message = "Unauthorized access." });

            var user = _context.Users.FirstOrDefault(u => u.UserID == id);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (ProfileImage == null || ProfileImage.Length == 0)
                return Json(new { success = false, message = "No file selected." });

            try {
                if (!string.IsNullOrEmpty(user.ProfileImage)) {
                    var oldPath = Path.Combine("wwwroot", user.ProfileImage.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ProfileImage.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await ProfileImage.CopyToAsync(stream);

                user.ProfileImage = $"/uploads/profiles/{fileName}";
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Profile photo updated successfully!", profileImageUrl = user.ProfileImage });
            } catch (Exception ex) {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }


    }

}

