using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetCloud.DTOs.Auth;
using PetCloud.DTOs.Common;
using PetCloud.Infrastructure;
using PetCloud.Models;
using PetCloud.Services;
using System.Text.RegularExpressions;

namespace PetCloud.Controllers.Api.V1 {
    [ApiController]
    [Route("api/v1/auth")]
    [Tags("Authentication")]
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
        [EndpointSummary("Log in")]
        [EndpointDescription("Authenticate with email and password. Returns JWT tokens directly, or a 2FA challenge if verification is required.")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request) {
            var currentIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var currentDevice = request.DeviceInfo ?? Request.Headers["User-Agent"].ToString();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                return Unauthorized(new ApiErrorResponse { Message = "Invalid email or password." });

            if (user.Type != "Owner")
                return Unauthorized(new ApiErrorResponse { Message = "You're trying to log in as Admin/Staff. Please use the web portal instead." });

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

                string subject = "Your PetCloud Login Code";
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
        [EndpointSummary("Verify 2FA code")]
        [EndpointDescription("Submit the 6-digit code sent to the user's email to complete two-factor authentication and receive JWT tokens.")]
        [ProducesResponseType(typeof(ApiResponse<TokenResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
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
        [EndpointSummary("Register a new owner")]
        [EndpointDescription("Create a new pet owner account. The account is immediately active and the user can log in after registration.")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
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
        [EndpointSummary("Refresh access token")]
        [EndpointDescription("Exchange a valid refresh token for a new access/refresh token pair. The old refresh token is revoked (rotation).")]
        [ProducesResponseType(typeof(ApiResponse<TokenResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
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
        [EndpointSummary("Log out")]
        [EndpointDescription("Revoke the provided refresh token, ending the session on this device.")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request) {
            await _jwtService.RevokeRefreshTokenAsync(request.RefreshToken);
            return Ok(new ApiResponse { Success = true, Message = "Logged out successfully." });
        }

        [HttpPost("forgot-password")]
        [EndpointSummary("Request password reset")]
        [EndpointDescription("Send a password reset link to the user's email address. The link expires in 1 hour.")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
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
        [EndpointSummary("Reset password")]
        [EndpointDescription("Set a new password using the token from the password reset email. The new password cannot match the current one.")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
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
