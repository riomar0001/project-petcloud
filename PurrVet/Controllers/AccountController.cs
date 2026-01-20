using System;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph.Models.TermStore;
using PurrVet.Models;
using PurrVet.Services;
using System.Text.Json;

namespace PurrVet.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;

        public AccountController(ApplicationDbContext context, IPasswordHasher<User> passwordHasher, EmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _configuration = configuration;
        }
        private void CreateNotification(string type, string message)
        {
            var notif = new Notification
            {
                Type = type,
                Message = message,
                CreatedAt = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(notif);

            _context.SaveChanges();
        }

        public IActionResult Home() => View();

        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, [FromForm(Name = "g-recaptcha-response")] string gRecaptcha, bool rememberMe = false)
        {
          var secretKey = _configuration["GoogleReCaptcha:SecretKey"];
          var httpClient = new HttpClient();

          var googleReply = await httpClient.GetStringAsync(
               $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={gRecaptcha}"
            );

           var captchaResult = JsonSerializer.Deserialize<ReCaptchaResponse>(googleReply);

            if (captchaResult == null || !captchaResult.success)
              return Json(new { success = false, message = "reCAPTCHA verification failed." });

            var currentIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var currentDevice = Request.Headers["User-Agent"].ToString();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return Json(new { success = false, message = "Invalid email or password." });

            if (user.Status == "Inactive")
            {
                return Json(new { success = false, message = "Your account is disabled due to inactivity. Please contact us at hpawsvetclinic@gmail.com\r\n" });
            }

            if (user.LastTwoFactorVerification.HasValue &&
                user.LastTwoFactorVerification.Value.AddDays(100) < DateTime.Now)
            {
                user.Status = "Inactive";
                await _context.SaveChangesAsync();
                return Json(new { success = false, message = "Your account has been inactive for over 100 days and has been disabled. Please contact us at hpawsvetclinic@gmail.com\r\n" });
            }

            if (!user.LastTwoFactorVerification.HasValue &&
                user.CreatedAt.AddDays(100) < DateTime.Now)
            {
                user.Status = "Inactive";
                await _context.SaveChangesAsync();
                return Json(new { success = false, message = "Your account has been inactive for over 100 days and has been disabled. Please contact us at hpawsvetclinic@gmail.com\r\n" });
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.Now)
            {
                var remaining = (user.LockoutEnd.Value - DateTime.Now).Minutes;
                return Json(new { success = false, message = $"Account locked. Try again in {remaining} minutes." });
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, password);
            if (result == PasswordVerificationResult.Failed)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockoutEnd = DateTime.Now.AddMinutes(3);
                    user.FailedLoginAttempts = 0;
                }
                await _context.SaveChangesAsync();
                return Json(new { success = false, message = "Invalid email or password." });
            }

            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;

            bool newDeviceOrLocation =
                user.LastLoginIP != currentIp || user.LastLoginDevice != currentDevice;

            bool requires2FA = user.TwoFactorEnabled &&
                               (
                                   !user.LastTwoFactorVerification.HasValue ||
                                   user.LastTwoFactorVerification.Value.AddDays(30) < DateTime.Now ||
                                   newDeviceOrLocation
                               );

            if (requires2FA)
            {
                var code = new Random().Next(100000, 999999).ToString();
                user.TwoFactorCode = code;
                user.TwoFactorExpiry = DateTime.Now.AddMinutes(10);

                await _context.SaveChangesAsync();

                string subject = "Your PurrVet Login Code";
                string body = $@"
            <h3>Hello {user.FirstName},</h3>
            <p>Your login code is:</p>
            <h2>{code}</h2>
            <p>This code will expire in 10 minutes.</p>";

                await _emailService.SendEmailAsync(user.Email, subject, "", body);

                return Json(new { success = true, requires2FA = true, userId = user.UserID });
            }

            user.LastTwoFactorVerification = DateTime.Now;
            user.LastLoginIP = currentIp;
            user.LastLoginDevice = currentDevice;
            await _context.SaveChangesAsync();

            await SignInUser(user);

            string redirectUrl = user.Type switch
            {
                "Admin" => Url.Action("Dashboard", "Admin"),
                "Owner" => Url.Action("Dashboard", "Owner"),
                "Staff" => Url.Action("Dashboard", "Staff"),
                _ => Url.Action("Home", "Account")
            };

            return Json(new { success = true, redirectUrl });
        }

        private async Task SignInUser(User user)
        {
            HttpContext.Session.SetString("ProfileImage", string.IsNullOrEmpty(user.ProfileImage) ? "golden.png" : Path.GetFileName(user.ProfileImage));
            HttpContext.Session.SetString("UserRole", user.Type);
            HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.LastName}");
            HttpContext.Session.SetInt32("UserID", user.UserID);

            if (user.Type == "Owner")
            {
                var owner = await _context.Owners.FirstOrDefaultAsync(o => o.UserID == user.UserID);
                if (owner != null) HttpContext.Session.SetInt32("OwnerID", owner.OwnerID);
            }

            var claims = new List<Claim>
    {
        new(ClaimTypes.Name, user.Email),
        new("UserID", user.UserID.ToString()),
        new("UserName", $"{user.FirstName} {user.LastName}"),
        new("UserRole", user.Type),
        new("ProfileImage", string.IsNullOrEmpty(user.ProfileImage) ? "golden.png" : Path.GetFileName(user.ProfileImage))
    };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) 
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
        }

        [HttpGet]
        public IActionResult Verify2FA(int userId)
        {
            ViewBag.UserId = userId;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Verify2FA(int userId, string code, bool rememberMe = false)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Json(new { success = false, message = "User not found." });

            if (user.TwoFactorCode != code || user.TwoFactorExpiry < DateTime.Now)
                return Json(new { success = false, message = "Invalid or expired code." });

            user.LastTwoFactorVerification = DateTime.Now;
            user.TwoFactorCode = null;
            user.TwoFactorExpiry = null;
            user.LastLoginIP = HttpContext.Connection.RemoteIpAddress?.ToString();
            user.LastLoginDevice = Request.Headers["User-Agent"].ToString();
            await _context.SaveChangesAsync();

            await SignInUser(user);

            string redirectUrl = user.Type switch
            {
                "Admin" => Url.Action("Dashboard", "Admin"),
                "Owner" => Url.Action("Dashboard", "Owner"),
                "Staff" => Url.Action("Dashboard", "Staff"),
                _ => Url.Action("Home", "Account")
            };

            return Json(new { success = true, redirectUrl });
        }

        [HttpPost]
        public IActionResult RefreshProfileImage()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return Json(new { success = false });

            var user = _context.Users.FirstOrDefault(u => u.UserID == userId);
            if (user == null) return Json(new { success = false });

            var imageFile = string.IsNullOrEmpty(user.ProfileImage) ? "golden.png" : Path.GetFileName(user.ProfileImage);
            HttpContext.Session.SetString("ProfileImage", imageFile);

            return Json(new { success = true, image = imageFile });
        }
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var userId = HttpContext.Session.GetInt32("UserID");

            if (!string.IsNullOrEmpty(userEmail) || userId != null)
            {
                var connection = _context.MicrosoftAccountConnections
                    .FirstOrDefault(x => x.MicrosoftEmail == userEmail || x.UserID == userId);

                if (connection != null)
                {
                    _context.MicrosoftAccountConnections.Remove(connection);
                    _context.SaveChanges();
                }
            }

            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);

            return RedirectToAction("Home", "Account");
        }

        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(string firstname, string lastname, string email, string phone, string password, string confirmPassword)
        {
            if (_context.Users.Any(u => u.Email == email))
                return Json(new { success = false, message = "Email already exists." });

            if (!Regex.IsMatch(firstname ?? "", @"^[a-zA-Z\s\\-]+$") || !Regex.IsMatch(lastname ?? "", @"^[a-zA-Z\s\\-]+$"))
                return Json(new { success = false, message = "Names must not contain special characters or numbers." });

            if (string.IsNullOrWhiteSpace(phone) || !Regex.IsMatch(phone, @"^\d{11}$"))
                return Json(new { success = false, message = "Phone number must be exactly 11 digits." });

            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return Json(new { success = false, message = "Password must be at least 8 characters long." });

            if (password != confirmPassword)
                return Json(new { success = false, message = "Passwords do not match." });

            var newUser = new User
            {
                FirstName = firstname,
                LastName = lastname,
                Email = email,
                Phone = phone,
                Type = "Owner",
                Status = "Active",
                ProfileImage = "pet.png"
            };

            newUser.Password = _passwordHasher.HashPassword(newUser, password);
            _context.Users.Add(newUser);
            _context.SaveChanges();

            var owner = new Owner
            {
                UserID = newUser.UserID,
                Name = $"{firstname} {lastname}",
                Email = email,
                Phone = phone
            };

            _context.Owners.Add(owner);
            _context.SystemLogs.Add(new SystemLog
            {
                ActionType = "Create",
                Module = "User",
                Description = $"A new user has signed up: {firstname} {lastname}",
                PerformedBy = $"UserID:{newUser.UserID}",
                Timestamp = DateTime.Now
            });
            _context.SaveChanges();

            CreateNotification("User", $"New owner registered: {firstname} {lastname}.");

            return Json(new { success = true, message = "Registration successful!" });
        }

        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email, [FromServices] EmailService emailService)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { success = false, message = "Please enter your email address." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return Json(new { success = false, message = "Email not found." });

            var token = Guid.NewGuid().ToString();
            user.ResetToken = token;
            user.TokenExpiry = DateTime.Now.AddHours(1);
            await _context.SaveChangesAsync();

            var resetLink = Url.Action("ResetPassword", "Account", new { token = token }, Request.Scheme);

            string subject = "Reset Your Password - Happy Paws Veterinary Clinic";
            string htmlBody = $@"
        <h3>Hello {user.FirstName},</h3>
        <p>You requested to reset your password. Click the link below to set a new one:</p>
        <p><a href='{resetLink}' style='background-color:#00b4d8;color:white;padding:10px 20px;text-decoration:none;border-radius:5px;'>Reset Password</a></p>
        <p>If you didn’t request this, please ignore this email.</p>
        <p>– Happy Paws Veterinary Clinic</p>";

            await emailService.SendEmailAsync(user.Email, subject, "", htmlBody);

            return Json(new { success = true, message = "Reset link sent! Please check your email." });
        }

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            var user = _context.Users.FirstOrDefault(u => u.ResetToken == token && u.TokenExpiry > DateTime.Now);
            if (user == null)
                return RedirectToAction("ForgotPassword");

            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult ResetPassword(string token, string newPassword, string confirmPassword)
        {
            var user = _context.Users.FirstOrDefault(u => u.ResetToken == token && u.TokenExpiry > DateTime.Now);
            if (user == null)
                return Json(new { success = false, message = "Invalid or expired reset token." });

            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
                return Json(new { success = false, message = "Please fill out all fields." });

            if (newPassword != confirmPassword)
                return Json(new { success = false, message = "Passwords do not match." });

            if (newPassword.Length < 8)
                return Json(new { success = false, message = "Password must be at least 8 characters long." });
            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password, newPassword);

            if (verificationResult == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success)
                return Json(new { success = false, message = "New password cannot be the same as the old password." });
            user.Password = _passwordHasher.HashPassword(user, newPassword);
            user.ResetToken = null;
            user.TokenExpiry = null;
            _context.SaveChanges();

            return Json(new { success = true, message = "Password reset successful! You can now log in." });
        }

    }
}
