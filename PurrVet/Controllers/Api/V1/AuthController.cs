using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PurrVet.DTOs.Auth;
using PurrVet.DTOs.Common;
using PurrVet.Infrastructure;
using PurrVet.Models;
using PurrVet.Services;
using System.Text.RegularExpressions;

namespace PurrVet.Controllers.Api.V1 {
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly JwtTokenService _jwtService;
        private readonly EmailService _emailService;

        public AuthController(
            ApplicationDbContext context,
            IPasswordHasher<User> passwordHasher,
            JwtTokenService jwtService,
            EmailService emailService) {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _emailService = emailService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request) {
            var currentIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var currentDevice = request.DeviceInfo ?? Request.Headers["User-Agent"].ToString();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                return Unauthorized(new ApiErrorResponse { Message = "Invalid email or password." });

            if (user.Type != "Owner")
                return Unauthorized(new ApiErrorResponse { Message = "This API is only available for pet owners." });

            if (user.Status == "Inactive")
                return Unauthorized(new ApiErrorResponse { Message = "Your account is disabled due to inactivity. Please contact support." });

            // Inactivity check (100 days)
            if (user.LastTwoFactorVerification.HasValue &&
                user.LastTwoFactorVerification.Value.AddDays(100) < DateTime.Now) {
                user.Status = "Inactive";
                await _context.SaveChangesAsync();
                return Unauthorized(new ApiErrorResponse { Message = "Your account has been inactive for over 100 days and has been disabled." });
            }

            if (!user.LastTwoFactorVerification.HasValue &&
                user.CreatedAt.AddDays(100) < DateTime.Now) {
                user.Status = "Inactive";
                await _context.SaveChangesAsync();
                return Unauthorized(new ApiErrorResponse { Message = "Your account has been inactive for over 100 days and has been disabled." });
            }

            // Lockout check
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.Now) {
                var remaining = (int)(user.LockoutEnd.Value - DateTime.Now).TotalMinutes + 1;
                return StatusCode(429, new ApiErrorResponse { Message = $"Account locked. Try again in {remaining} minute(s)." });
            }

            // Password verification
            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, request.Password);
            if (result == PasswordVerificationResult.Failed) {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5) {
                    user.LockoutEnd = DateTime.Now.AddMinutes(3);
                    user.FailedLoginAttempts = 0;
                }
                await _context.SaveChangesAsync();
                return Unauthorized(new ApiErrorResponse { Message = "Invalid email or password." });
            }

            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;

            // 2FA check
            bool newDeviceOrLocation = user.LastLoginIP != currentIp || user.LastLoginDevice != currentDevice;
            bool requires2FA = user.TwoFactorEnabled &&
                (!user.LastTwoFactorVerification.HasValue ||
                 user.LastTwoFactorVerification.Value.AddDays(30) < DateTime.Now ||
                 newDeviceOrLocation);

            if (requires2FA) {
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

                return Ok(new ApiResponse<LoginResponse> {
                    Success = true,
                    Message = "2FA code sent to your email.",
                    Data = new LoginResponse {
                        Requires2FA = true,
                        TwoFactorUserId = user.UserID
                    }
                });
            }

            // No 2FA required — issue tokens
            var owner = await _context.Owners.FirstOrDefaultAsync(o => o.UserID == user.UserID);
            if (owner == null)
                return Unauthorized(new ApiErrorResponse { Message = "Owner record not found." });

            user.LastTwoFactorVerification = DateTime.Now;
            user.LastLoginIP = currentIp;
            user.LastLoginDevice = currentDevice;
            await _context.SaveChangesAsync();

            var accessToken = _jwtService.GenerateAccessToken(user, owner.OwnerID);
            var refreshToken = _jwtService.GenerateRefreshToken();
            await _jwtService.SaveRefreshTokenAsync(user.UserID, refreshToken, currentDevice);

            return Ok(new ApiResponse<LoginResponse> {
                Success = true,
                Data = new LoginResponse {
                    Requires2FA = false,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                    Owner = new OwnerBasicDto {
                        UserId = user.UserID,
                        OwnerId = owner.OwnerID,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        ProfileImage = user.ProfileImage
                    }
                }
            });
        }

        [HttpPost("verify-2fa")]
        public async Task<IActionResult> Verify2FA([FromBody] Verify2FARequest request) {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
                return NotFound(new ApiErrorResponse { Message = "User not found." });

            if (user.Type != "Owner")
                return Unauthorized(new ApiErrorResponse { Message = "This API is only available for pet owners." });

            if (user.TwoFactorCode != request.Code || user.TwoFactorExpiry < DateTime.Now)
                return Unauthorized(new ApiErrorResponse { Message = "Invalid or expired code." });

            var owner = await _context.Owners.FirstOrDefaultAsync(o => o.UserID == user.UserID);
            if (owner == null)
                return Unauthorized(new ApiErrorResponse { Message = "Owner record not found." });

            var currentIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var currentDevice = request.DeviceInfo ?? Request.Headers["User-Agent"].ToString();

            user.LastTwoFactorVerification = DateTime.Now;
            user.TwoFactorCode = null;
            user.TwoFactorExpiry = null;
            user.LastLoginIP = currentIp;
            user.LastLoginDevice = currentDevice;
            await _context.SaveChangesAsync();

            var accessToken = _jwtService.GenerateAccessToken(user, owner.OwnerID);
            var refreshToken = _jwtService.GenerateRefreshToken();
            await _jwtService.SaveRefreshTokenAsync(user.UserID, refreshToken, currentDevice);

            return Ok(new ApiResponse<TokenResponse> {
                Success = true,
                Data = new TokenResponse {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                    Owner = new OwnerBasicDto {
                        UserId = user.UserID,
                        OwnerId = owner.OwnerID,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        ProfileImage = user.ProfileImage
                    }
                }
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request) {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return Conflict(new ApiErrorResponse { Message = "Email already exists." });

            if (!Regex.IsMatch(request.FirstName ?? "", @"^[a-zA-Z\s\-]+$") ||
                !Regex.IsMatch(request.LastName ?? "", @"^[a-zA-Z\s\-]+$"))
                return BadRequest(new ApiErrorResponse { Message = "Names must not contain special characters or numbers." });

            var newUser = new User {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                Type = "Owner",
                Status = "Active",
                ProfileImage = "pet.png"
            };

            newUser.Password = _passwordHasher.HashPassword(newUser, request.Password);
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            var owner = new Owner {
                UserID = newUser.UserID,
                Name = $"{request.FirstName} {request.LastName}",
                Email = request.Email,
                Phone = request.Phone
            };

            _context.Owners.Add(owner);
            _context.SystemLogs.Add(new SystemLog {
                ActionType = "Create",
                Module = "User",
                Description = $"A new user has signed up via mobile: {request.FirstName} {request.LastName}",
                PerformedBy = $"UserID:{newUser.UserID}",
                Timestamp = DateTime.Now
            });
            await _context.SaveChangesAsync();

            _context.Notifications.Add(new Notification {
                Type = "User",
                Message = $"New owner registered: {request.FirstName} {request.LastName}.",
                CreatedAt = DateTime.Now,
                IsRead = false
            });
            await _context.SaveChangesAsync();

            return StatusCode(201, new ApiResponse { Success = true, Message = "Registration successful!" });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request) {
            var (success, newAccessToken, newRefreshToken, expiresAt, error) =
                await _jwtService.RotateRefreshTokenAsync(request.RefreshToken);

            if (!success)
                return Unauthorized(new ApiErrorResponse { Message = error! });

            return Ok(new ApiResponse<TokenResponse> {
                Success = true,
                Data = new TokenResponse {
                    AccessToken = newAccessToken!,
                    RefreshToken = newRefreshToken!,
                    ExpiresAt = expiresAt!.Value
                }
            });
        }

        [HttpPost("logout")]
        [Authorize(AuthenticationSchemes = "Bearer", Policy = "OwnerOnly")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request) {
            await _jwtService.RevokeRefreshTokenAsync(request.RefreshToken);
            return Ok(new ApiResponse { Success = true, Message = "Logged out successfully." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request) {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                return NotFound(new ApiErrorResponse { Message = "Email not found." });

            var token = Guid.NewGuid().ToString();
            user.ResetToken = token;
            user.TokenExpiry = DateTime.Now.AddHours(1);
            await _context.SaveChangesAsync();

            var resetLink = $"{Request.Scheme}://{Request.Host}/Account/ResetPassword?token={token}";

            string subject = "Reset Your Password - Happy Paws Veterinary Clinic";
            string htmlBody = $@"
                <h3>Hello {user.FirstName},</h3>
                <p>You requested to reset your password. Click the link below to set a new one:</p>
                <p><a href='{resetLink}' style='background-color:#00b4d8;color:white;padding:10px 20px;text-decoration:none;border-radius:5px;'>Reset Password</a></p>
                <p>If you didn't request this, please ignore this email.</p>
                <p>– Happy Paws Veterinary Clinic</p>";

            await _emailService.SendEmailAsync(user.Email, subject, "", htmlBody);

            return Ok(new ApiResponse { Success = true, Message = "Reset link sent! Please check your email." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request) {
            var user = await _context.Users.FirstOrDefaultAsync(
                u => u.ResetToken == request.Token && u.TokenExpiry > DateTime.Now);

            if (user == null)
                return BadRequest(new ApiErrorResponse { Message = "Invalid or expired reset token." });

            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password, request.NewPassword);
            if (verificationResult == PasswordVerificationResult.Success)
                return BadRequest(new ApiErrorResponse { Message = "New password cannot be the same as the old password." });

            user.Password = _passwordHasher.HashPassword(user, request.NewPassword);
            user.ResetToken = null;
            user.TokenExpiry = null;
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse { Success = true, Message = "Password reset successful! You can now log in." });
        }
    }
}
