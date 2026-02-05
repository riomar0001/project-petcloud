using CsvHelper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using PurrVet.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System.Globalization;
using System.Text.RegularExpressions;
namespace PurrVet.Controllers {
    public class OwnerController : Controller {
        private readonly ApplicationDbContext _context;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly IWebHostEnvironment _hostEnvironment;

        public OwnerController(ApplicationDbContext context, GraphServiceClient graphServiceClient, ITokenAcquisition tokenAcquisition, IWebHostEnvironment hostEnvironment) {
            _context = context;
            _graphServiceClient = graphServiceClient;
            _tokenAcquisition = tokenAcquisition;
            _hostEnvironment = hostEnvironment;

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
                                .Select(r => (r as IDictionary<string, object>)?["Breed"]?.ToString())
                                .Where(b => !string.IsNullOrWhiteSpace(b))
                                .Distinct()
                                .OrderBy(b => b)
                                .ToList();
                return Json(breeds);
            }
        }
        public IActionResult Dashboard() {
            if (HttpContext.Session.GetString("UserRole") != "Owner")
                return RedirectToAction("Login", "Account");

            var ownerId = HttpContext.Session.GetInt32("OwnerID");
            if (ownerId == null) return RedirectToAction("Login", "Account");

            var vm = new OwnerDashboardViewModel {
                UserName = HttpContext.Session.GetString("UserName"),

                Pets = _context.Pets
                    .Where(p => p.OwnerID == ownerId)
                    .OrderByDescending(p => p.PetID)
                    .Select(p => new {
                        p.PetID,
                        p.Name,
                        p.Breed,
                        p.PhotoPath,
                        p.Birthdate
                    }).ToList<dynamic>(),

                UpcomingAppointments = _context.Appointments
                .Include(a => a.Pet)
                .Include(a => a.ServiceCategory)
                .Where(a => a.Pet.OwnerID == ownerId &&
                            (a.Status == "Pending" || a.AppointmentDate >= DateTime.Now))
                .OrderBy(a => a.AppointmentDate)
                .Select(a => new {
                    a.AppointmentID,
                    a.AppointmentDate,
                    a.Status,
                    Pet = new { a.Pet.PetID, a.Pet.Name },
                    ServiceCategory = new { a.ServiceCategory.ServiceType }
                })
                .ToList<dynamic>(),


                VaccineDue = _context.Appointments
                    .Include(a => a.Pet)
                    .Include(a => a.ServiceCategory)
                    .Where(a => a.Pet.OwnerID == ownerId &&
                                a.DueDate != null &&
                                a.DueDate <= DateTime.Now.AddDays(5) &&
                                a.ServiceCategory.ServiceType.Contains("Vaccination"))
                    .OrderBy(a => a.DueDate)
                    .Select(a => new {
                        a.AppointmentID,
                        a.DueDate,
                        Pet = new { a.Pet.PetID, a.Pet.Name },
                        ServiceCategory = new { a.ServiceCategory.ServiceType }
                    }).ToList<dynamic>(),

                DewormDue = _context.Appointments
                    .Include(a => a.Pet)
                    .Include(a => a.ServiceCategory)
                    .Where(a => a.Pet.OwnerID == ownerId &&
                                a.DueDate != null &&
                                a.DueDate <= DateTime.Now.AddDays(5) &&
                                a.ServiceCategory.ServiceType.Contains("Deworming & Preventives"))
                    .OrderBy(a => a.DueDate)
                    .Select(a => new {
                        a.AppointmentID,
                        a.DueDate,
                        Pet = new { a.Pet.PetID, a.Pet.Name },
                        ServiceCategory = new { a.ServiceCategory.ServiceType }
                    }).ToList<dynamic>()
            };

            return View(vm);
        }


        public IActionResult Pets() {
            if (HttpContext.Session.GetString("UserRole") != "Owner")
                return RedirectToAction("Login", "Account");
            var ownerId = HttpContext.Session.GetInt32("OwnerID");
            var pets = _context.Pets
                               .Where(p => p.OwnerID == ownerId)
                               .ToList();

            return View(pets);
        }
        [HttpGet]
        public IActionResult Appointments() {
            if (HttpContext.Session.GetString("UserRole") != "Owner")
                return RedirectToAction("Login", "Account");

            var ownerId = HttpContext.Session.GetInt32("OwnerID");
            if (ownerId == null)
                return RedirectToAction("Login", "Account");

            var allAppointments = _context.Appointments
                .Include(a => a.Pet)
                .ThenInclude(p => p.Owner)
                .Include(a => a.ServiceCategory)
                .Include(a => a.ServiceSubtype)
                .ToList();

            var appointmentsForView = allAppointments.Select(a => new Appointment {
                AppointmentID = a.AppointmentID,
                AppointmentDate = a.AppointmentDate,
                Status = a.Pet.OwnerID == ownerId ? a.Status : "Not Available",
                GroupID = a.GroupID,
                Notes = a.Pet.OwnerID == ownerId ? a.Notes : "Booked by another owner",
                Pet = a.Pet,
                ServiceCategory = a.ServiceCategory,
                ServiceSubtype = a.ServiceSubtype
            }).ToList();

            var pendingGroupCount = allAppointments
                .Where(a => a.Pet.OwnerID == ownerId && a.Status == "Pending" && a.GroupID != null)
                .Select(a => a.GroupID)
                .Distinct()
                .Count();

            ViewBag.PendingGroupCount = pendingGroupCount;

            return View(appointmentsForView);
        }


        [HttpGet]
        public IActionResult AddAppointment() {
            if (HttpContext.Session.GetString("UserRole") != "Owner")
                return RedirectToAction("Login", "Account");

            var ownerId = HttpContext.Session.GetInt32("OwnerID");
            if (ownerId == null)
                return RedirectToAction("Login", "Account");

            ViewBag.Pets = _context.Pets
                .Where(p => p.OwnerID == ownerId)
                .Select(p => new {
                    p.PetID,
                    p.Name,
                    p.Type
                })
                .ToList();

            ViewBag.ServiceCategories = _context.ServiceCategories
                .Select(c => new { c.CategoryID, c.ServiceType })
                .ToList();

            ViewBag.ServiceSubtypes = _context.ServiceSubtypes
                .Select(s => new { s.SubtypeID, s.ServiceSubType, s.CategoryID })
                .ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddAppointmentsBulkOwner([FromForm] AppointmentBulkForm form) {
            if (HttpContext.Session.GetString("UserRole") != "Owner")
                return Json(new { success = false, message = "Unauthorized access." });

            var ownerId = HttpContext.Session.GetInt32("OwnerID");
            if (ownerId == null)
                return Json(new { success = false, message = "Session expired. Please log in again." });

            if (form.Appointments == null || form.Appointments.Count == 0)
                return Json(new { success = false, message = "No appointments provided." });

            var firstValid = form.Appointments.FirstOrDefault(a =>
                a.PetID != 0 && a.CategoryID != null &&
                a.AppointmentDate != default && !string.IsNullOrEmpty(a.AppointmentTime));

            if (firstValid == null)
                return Json(new { success = false, message = "All services must have completed fields." });

            if (!TimeSpan.TryParse(firstValid.AppointmentTime, out var parsedTime))
                return Json(new { success = false, message = "Invalid time format." });

            var groupDateTime = firstValid.AppointmentDate.Date.Add(parsedTime);

            var group = new AppointmentGroup {
                GroupTime = groupDateTime,
                Notes = "Grouped appointment (Owner)",
                CreatedAt = DateTime.Now
            };

            _context.AppointmentGroups.Add(group);
            _context.SaveChanges();

            var added = new List<Appointment>();

            foreach (var a in form.Appointments) {
                if (a.PetID == 0 || a.CategoryID == null)
                    continue;

                var pet = _context.Pets.FirstOrDefault(p => p.PetID == a.PetID && p.OwnerID == ownerId);
                if (pet == null)
                    continue;

                var appointment = new Appointment {
                    PetID = a.PetID,
                    CategoryID = a.CategoryID,
                    SubtypeID = a.SubtypeID,
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
                return Json(new { success = false, message = "No valid appointments to add." });

            _context.SaveChanges();

            var userName = HttpContext.Session.GetString("UserName") ?? "Owner";
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
                Description = $"Owner #{ownerId} created {added.Count} services in Group #{group.GroupID} scheduled for {dateText}.",
                PerformedBy = userName,
                Timestamp = DateTime.Now
            });

            _context.SaveChanges();

            return Json(new {
                success = true,
                message = $"Successfully added {added.Count} service(s) (Group #{group.GroupID}).",
                groupId = group.GroupID
            });
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddAppointment(Appointment model, string AppointmentTime) {
            if (HttpContext.Session.GetString("UserRole") != "Owner")
                return Json(new { success = false, message = "Unauthorized access." });

            var ownerId = HttpContext.Session.GetInt32("OwnerID");
            if (ownerId == null)
                return Json(new { success = false, message = "Session expired. Please log in again." });

            if (model.PetID == 0 || model.CategoryID == null || model.AppointmentDate == default)
                return Json(new { success = false, message = "All required fields must be filled." });

            if (TimeSpan.TryParse(AppointmentTime, out var parsedTime))
                model.AppointmentDate = model.AppointmentDate.Date.Add(parsedTime);

            bool taken = _context.Appointments.Any(a => a.AppointmentDate == model.AppointmentDate);
            if (taken)
                return Json(new { success = false, message = "This time slot is already taken." });

            model.Status = "Pending";
            model.CreatedAt = DateTime.Now;

            try {
                _context.Appointments.Add(model);
                _context.Notifications.Add(new Notification {
                    Message = $"New appointment requested by Owner #{ownerId} for Pet ID: {model.PetID} on {model.AppointmentDate:MMM dd, yyyy hh:mm tt}.",
                    Type = "Appointment",
                    TargetRole = "Staff"
                });

                _context.SaveChanges();

                return Json(new { success = true, message = "Appointment added successfully!" });
            } catch (Exception ex) {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }


        [HttpGet]
        public IActionResult GetAvailableTimeSlots(DateTime date) {
            var ownerId = HttpContext.Session.GetInt32("OwnerID");
            if (ownerId == null)
                return Json(new { success = false, message = "Unauthorized. Please log in again." });

            var start = new TimeSpan(9, 0, 0);
            var end = new TimeSpan(18, 0, 0);
            var interval = TimeSpan.FromMinutes(5);
            var slots = new List<object>();

            var now = DateTime.Now;
            bool isToday = date.Date == now.Date;

            var taken = _context.Appointments
                .Where(a => a.AppointmentDate.Date == date.Date && a.Status.ToLower() != "cancelled")
                .Select(a => new TimeSpan(a.AppointmentDate.Hour, a.AppointmentDate.Minute, 0))
                .ToList();

            for (var t = start; t <= end; t = t.Add(interval)) {
                bool isTaken = taken.Any(x => Math.Abs((x - t).TotalMinutes) < 1);

                if (isToday && DateTime.Today.Add(t) < now) {
                    isTaken = true;
                }

                slots.Add(new {
                    time = DateTime.Today.Add(t).ToString("HH:mm"),
                    available = !isTaken,
                    label = isTaken ? "Not Available" : "Available"
                });
            }

            return Json(new { success = true, date = date.ToString("yyyy-MM-dd"), slots });
        }

        [HttpPost("Owner/RequestCancellation/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult RequestCancellation(int id) {
            var appointment = _context.Appointments
                .FirstOrDefault(a => a.AppointmentID == id);

            if (appointment == null)
                return Json(new { success = false, message = "Appointment not found." });

            if (appointment.GroupID == null)
                return Json(new { success = false, message = "This appointment is not part of a group." });

            var groupAppointments = _context.Appointments
                .Where(a => a.GroupID == appointment.GroupID)
                .ToList();

            if (!groupAppointments.Any())
                return Json(new { success = false, message = "No appointments found for this group." });

            var invalidStatuses = groupAppointments
                .Where(a => a.Status.ToLower() != "pending" && a.Status.ToLower() != "requested")
                .ToList();

            if (invalidStatuses.Any())
                return Json(new {
                    success = false,
                    message = "This group contains appointments that cannot be cancelled."
                });

            foreach (var appt in groupAppointments) {
                appt.Status = "Cancellation Requested";
                _context.Appointments.Update(appt);
            }

            _context.SystemLogs.Add(new SystemLog {
                ActionType = "Update",
                Module = "Appointment",
                Description = $"Owner requested cancellation for group #{appointment.GroupID} ({groupAppointments.Count} appointments).",
                PerformedBy = HttpContext.Session.GetString("UserName") ?? "Owner",
                Timestamp = DateTime.Now
            });

            _context.SaveChanges();

            return Json(new {
                success = true,
                message = $"Cancellation request sent for Group #{appointment.GroupID} ({groupAppointments.Count} appointments)."
            });
        }

        [HttpGet]
        public IActionResult Add() {
            if (HttpContext.Session.GetString("UserRole") != "Owner")
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(string Name, string Type, string Breed, DateTime Birthdate, IFormFile? Photo) {
            if (HttpContext.Session.GetString("UserRole") != "Owner")
                return Json(new { success = false, message = "Unauthorized access." });

            var ownerId = HttpContext.Session.GetInt32("OwnerID");
            if (ownerId == null)
                return Json(new { success = false, message = "Owner not found in session." });

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
                    OwnerID = ownerId.Value,
                    Name = Name,
                    Type = Type,
                    Breed = string.IsNullOrWhiteSpace(Breed) ? "N/A" : Breed,
                    Birthdate = Birthdate,
                    PhotoPath = photoPath
                };

                _context.Pets.Add(pet);
                _context.Notifications.Add(new Notification {
                    Message = $"A new pet '{pet.Name}' has been added by Owner ID:{pet.OwnerID}.",
                    Type = "Pet",
                    TargetRole = "Staff"
                });
                _context.SystemLogs.Add(new SystemLog {
                    ActionType = "Create",
                    Module = "Pet",
                    Description = $"Added a Pet: {pet.PetID}, {pet.Name}",
                    PerformedBy = HttpContext.Session.GetString("UserName") ?? "Unknown",
                    Timestamp = DateTime.Now
                });

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Pet added successfully!" });
            } catch (Exception ex) {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }

        }
        [HttpGet]
        public IActionResult Profile() {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserID");
            var ownerId = HttpContext.Session.GetInt32("OwnerID");

            if (string.IsNullOrEmpty(role) || userId == null || ownerId == null)
                return RedirectToAction("Login", "Account");

            if (role != "Owner")
                return RedirectToAction("Login", "Account");

            var ownerUser = _context.Users.FirstOrDefault(u => u.UserID == userId && u.Type == "Owner");
            if (ownerUser == null)
                return NotFound();

            var owner = _context.Owners.FirstOrDefault(o => o.OwnerID == ownerId);
            if (owner == null)
                return NotFound();

            var msConnection = _context.MicrosoftAccountConnections.FirstOrDefault(c => c.UserID == userId);
            if (msConnection != null)
                HttpContext.Session.SetString("MicrosoftEmail", msConnection.MicrosoftEmail);
            else
                HttpContext.Session.Remove("MicrosoftEmail");

            ViewBag.ConnectedMicrosoftEmail = msConnection?.MicrosoftEmail;
            ViewBag.AccessToken = msConnection?.AccessToken;
            ViewBag.RefreshToken = msConnection?.RefreshToken;
            ViewBag.TokenExpiry = msConnection?.TokenExpiry;
            ViewBag.MicrosoftConnected = TempData["MicrosoftConnected"] as bool? ?? false;

            return View(ownerUser);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditOwnerDetails(
            int id,
            string FirstName,
            string LastName,
            string Phone,
            string CurrentPassword,
            string Password,
            string ConfirmPassword) {
            if (HttpContext.Session.GetString("UserRole") != "Owner")
                return Json(new { success = false, message = "Unauthorized access." });

            var user = _context.Users.FirstOrDefault(u => u.UserID == id);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            var owner = _context.Owners.FirstOrDefault(o => o.UserID == id);
            if (owner == null)
                return Json(new { success = false, message = "Owner record not found." });

            if (!Regex.IsMatch(FirstName ?? "", @"^[a-zA-Z\s\\-]+$") || !Regex.IsMatch(LastName ?? "", @"^[a-zA-Z\s\\-]+$"))
                return Json(new { success = false, message = "Names must not contain special characters or numbers." });

            if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
                return Json(new { success = false, message = "First name and last name are required." });

            string fullName = $"{FirstName.Trim()} {LastName.Trim()}".Trim();

            if (string.IsNullOrWhiteSpace(Phone) || !Regex.IsMatch(Phone, @"^\d{11}$"))
                return Json(new { success = false, message = "Phone number must be exactly 11 digits." });

            user.FirstName = FirstName.Trim();
            user.LastName = LastName.Trim();
            user.Phone = Phone?.Trim();

            owner.Name = fullName;
            owner.Phone = Phone?.Trim();
            owner.Email = user.Email;

            if (!string.IsNullOrWhiteSpace(Password)) {
                if (string.IsNullOrWhiteSpace(CurrentPassword))
                    return Json(new { success = false, message = "Enter your current password to change it." });

                var hasher = new PasswordHasher<User>();
                var verify = hasher.VerifyHashedPassword(user, user.Password, CurrentPassword);
                if (verify == PasswordVerificationResult.Failed)
                    return Json(new { success = false, message = "Incorrect current password." });

                if (Password != ConfirmPassword)
                    return Json(new { success = false, message = "Passwords do not match." });

                if (Password.Length < 8)
                    return Json(new { success = false, message = "Password must be at least 8 characters." });

                user.Password = hasher.HashPassword(user, Password);
            }

            try {
                _context.Users.Update(user);
                _context.Owners.Update(owner);
                _context.SystemLogs.Add(new SystemLog {
                    ActionType = "Update",
                    Module = "Owner",
                    Description = $"Updated owner profile: {owner.Name}",
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
        public async Task<IActionResult> EditOwnerPhoto(int id, IFormFile? ProfileImage) {
            if (HttpContext.Session.GetString("UserRole") != "Owner")
                return Json(new { success = false, message = "Unauthorized access." });

            var user = _context.Users.FirstOrDefault(u => u.UserID == id);
            var owner = _context.Owners.FirstOrDefault(o => o.UserID == id);
            if (user == null || owner == null)
                return Json(new { success = false, message = "Owner not found." });

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

                var newPath = $"/uploads/profiles/{fileName}";
                user.ProfileImage = newPath;
                owner.User.ProfileImage = newPath;

                _context.Users.Update(user);
                _context.Owners.Update(owner);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Profile photo updated successfully!", profileImageUrl = newPath });
            } catch (Exception ex) {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
        [HttpGet]
        public IActionResult ViewPet(int id, int page = 1, string searchQuery = "", int? categoryFilter = null) {
            if (HttpContext.Session.GetString("UserRole") != "Owner")
                return RedirectToAction("Login", "Account");

            var ownerId = HttpContext.Session.GetInt32("OwnerID");
            if (ownerId == null)
                return RedirectToAction("Login", "Account");

            var pet = _context.Pets
                .Include(p => p.Owner)
                .FirstOrDefault(p => p.PetID == id);

            if (pet == null)
                return NotFound();

            if (pet.OwnerID != ownerId)
                return RedirectToAction("AccessDenied", "Account");

            int pageSize = 5;

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
        public IActionResult EditPet(int id) {
            if (HttpContext.Session.GetString("UserRole") != "Owner")
                return RedirectToAction("Login", "Account");

            var ownerId = HttpContext.Session.GetInt32("OwnerID");
            if (ownerId == null)
                return RedirectToAction("Login", "Account");

            var pet = _context.Pets
                .Include(p => p.Owner)
                .FirstOrDefault(p => p.PetID == id);

            if (pet == null)
                return NotFound();

            if (pet.OwnerID != ownerId)
                return RedirectToAction("AccessDenied", "Account");

            return View(pet);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPet(int id, string Name, string Type, string Breed, DateTime? Birthdate, IFormFile? Photo) {
            if (HttpContext.Session.GetString("UserRole") != "Owner")
                return Json(new { success = false, message = "Unauthorized access." });

            var ownerId = HttpContext.Session.GetInt32("OwnerID");
            if (ownerId == null) return Json(new { success = false, message = "Owner not in session." });

            var pet = _context.Pets.FirstOrDefault(p => p.PetID == id && p.OwnerID == ownerId);
            if (pet == null) return Json(new { success = false, message = "Pet not found." });

            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Type))
                return Json(new { success = false, message = "Name and Type are required." });

            try {
                pet.Name = Name.Trim();
                pet.Type = Type.Trim();
                pet.Breed = string.IsNullOrWhiteSpace(Breed) ? "N/A" : Breed.Trim();
                if (Birthdate.HasValue) pet.Birthdate = Birthdate.Value;

                if (Photo != null && Photo.Length > 0) {
                    if (!string.IsNullOrEmpty(pet.PhotoPath)) {
                        var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", pet.PhotoPath.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath)) {
                            try { System.IO.File.Delete(oldPath); } catch { }
                        }
                    }

                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "pets");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(Photo.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create)) {
                        await Photo.CopyToAsync(stream);
                    }

                    pet.PhotoPath = $"/uploads/pets/{fileName}";
                }

                _context.Pets.Update(pet);
                _context.SystemLogs.Add(new SystemLog {
                    ActionType = "Update",
                    Module = "Pet",
                    Description = $"Updated pet: {pet.PetID} - {pet.Name}",
                    PerformedBy = HttpContext.Session.GetString("UserName") ?? "Unknown",
                    Timestamp = DateTime.Now
                });

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Pet updated successfully!", redirect = Url.Action("ViewPet", "Owner", new { id = pet.PetID }) });
            } catch (Exception ex) {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
        [HttpGet]
        public IActionResult PetCard(int id, int page = 1) {
            if (HttpContext.Session.GetString("UserRole") != "Owner")
                return RedirectToAction("Login", "Account");

            var ownerId = HttpContext.Session.GetInt32("OwnerID");
            if (ownerId == null)
                return RedirectToAction("Login", "Account");

            var pet = _context.Pets
             .Include(p => p.Owner)
             .Include(p => p.Appointments)
                 .ThenInclude(a => a.ServiceCategory)
             .Include(p => p.Appointments)
                 .ThenInclude(a => a.ServiceSubtype)
             .FirstOrDefault(p =>
                 p.PetID == id &&
                 p.OwnerID == ownerId);



            if (pet == null)
                return RedirectToAction("Pets");

            if (pet.OwnerID != ownerId)
                return RedirectToAction("AccessDenied", "Account");

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

            int ageMonths = (int)Math.Floor(
                (DateTime.Now - pet.Birthdate).TotalDays / 30.4375
            );

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
        public JsonResult GetOwnerNotifications() {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return Json(new { notifications = new List<object>(), totalUnread = 0 });

            var query = _context.Notifications
                .Where(n => n.TargetUserId == userId || n.TargetRole == "Owner");

            var notifications = query
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .Select(n => new {
                    notificationID = n.NotificationID,
                    message = n.Message,
                    type = n.Type,
                    createdAt = n.CreatedAt.ToString("MMM dd, yyyy hh:mm tt"),
                    isRead = n.IsRead,
                    redirectUrl = n.RedirectUrl
                })
                .ToList();

            var unreadCount = query.Count(n => !n.IsRead);

            return Json(new {
                notifications,
                totalUnread = unreadCount
            });
        }

        [HttpPost]
        public IActionResult MarkAllOwnerNotificationsRead() {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return Json(new { success = false, message = "User not found" });

            var unread = _context.Notifications
                .Where(n => (n.TargetUserId == userId || n.TargetRole == "Owner") && !n.IsRead)
                .ToList();

            if (!unread.Any())
                return Json(new { success = false, message = "No unread notifications" });

            foreach (var n in unread)
                n.IsRead = true;

            _context.SaveChanges();

            return Json(new { success = true, message = "All notifications marked as read" });
        }

        [HttpPost]
        public IActionResult MarkOwnerNotificationRead(int id) {
            var userId = HttpContext.Session.GetInt32("UserID");
            var notif = _context.Notifications
                .FirstOrDefault(n => n.NotificationID == id &&
                                     (n.TargetUserId == userId || n.TargetRole == "Owner"));

            if (notif == null) return NotFound();

            notif.IsRead = true;
            _context.SaveChanges();

            return Ok();
        }

        [HttpGet]
        public IActionResult OwnerNotification(string typeFilter = "All", string statusFilter = "All", string searchQuery = "", int page = 1) {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToAction("Login", "Account");

            const int pageSize = 10;

            var query = _context.Notifications
                .Where(n => n.TargetUserId == userId || n.TargetRole == "Owner");

            if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "All")
                query = query.Where(n => n.Type == typeFilter);

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All") {
                if (statusFilter == "Read") query = query.Where(n => n.IsRead);
                else if (statusFilter == "Unread") query = query.Where(n => !n.IsRead);
            }

            if (!string.IsNullOrEmpty(searchQuery))
                query = query.Where(n => n.Message.Contains(searchQuery));

            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var notifications = query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationViewModel {
                    NotificationID = n.NotificationID,
                    Message = n.Message,
                    Type = n.Type,
                    CreatedAt = n.CreatedAt,
                    IsRead = n.IsRead,
                    RedirectUrl = n.RedirectUrl
                })
                .ToList();

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
    }
}
