using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetCloud.DTOs.Common;
using PetCloud.DTOs.Profile;
using PetCloud.Infrastructure;
using PetCloud.Models;
using System.Text.RegularExpressions;

namespace PetCloud.Controllers.Api.V1 {
    [ApiController]
    [Route("api/v1/profile")]
    [Authorize(Policy = "OwnerOnly")]
    [Tags("Profile")]
    public class ProfileController : ControllerBase {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public ProfileController(ApplicationDbContext context, IPasswordHasher<User> passwordHasher) {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        [EndpointSummary("Get owner profile")]
        [EndpointDescription("Returns the authenticated owner's profile including name, email, phone, and profile image URL.")]
        [ProducesResponseType(typeof(ApiResponse<ProfileResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile() {
            var userId = User.GetUserId();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId && u.Type == "Owner");
            if (user == null)
                return NotFound(new ApiErrorResponse { Message = "User not found." });

            var owner = await _context.Owners.FirstOrDefaultAsync(o => o.UserID == userId);
            if (owner == null)
                return NotFound(new ApiErrorResponse { Message = "Owner record not found." });

            var profileImageUrl = !string.IsNullOrEmpty(user.ProfileImage) && user.ProfileImage.StartsWith("/")
                ? baseUrl + user.ProfileImage
                : null;

            return Ok(new ApiResponse<ProfileResponse> {
                Success = true,
                Data = new ProfileResponse {
                    UserId = user.UserID,
                    OwnerId = owner.OwnerID,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.Phone,
                    ProfileImageUrl = profileImageUrl,
                    CreatedAt = user.CreatedAt
                }
            });
        }

        [HttpPut]
        [EndpointSummary("Update owner profile")]
        [EndpointDescription("Update the owner's first name, last name, and phone number. Names must contain only letters, spaces, and hyphens.")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request) {
            var userId = User.GetUserId();
            var userName = User.GetUserName();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null)
                return NotFound(new ApiErrorResponse { Message = "User not found." });

            var owner = await _context.Owners.FirstOrDefaultAsync(o => o.UserID == userId);
            if (owner == null)
                return NotFound(new ApiErrorResponse { Message = "Owner record not found." });

            if (!Regex.IsMatch(request.FirstName ?? "", @"^[a-zA-Z\s\-]+$") ||
                !Regex.IsMatch(request.LastName ?? "", @"^[a-zA-Z\s\-]+$"))
                return BadRequest(new ApiErrorResponse { Message = "Names must not contain special characters or numbers." });

            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.Phone = request.Phone.Trim();

            string fullName = $"{user.FirstName} {user.LastName}";
            owner.Name = fullName;
            owner.Phone = request.Phone.Trim();
            owner.Email = user.Email;

            _context.Users.Update(user);
            _context.Owners.Update(owner);
            _context.SystemLogs.Add(new SystemLog {
                ActionType = "Update",
                Module = "Owner",
                Description = $"Updated owner profile: {fullName} (via mobile)",
                PerformedBy = userName,
                Timestamp = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Ok(new ApiResponse { Success = true, Message = "Profile updated successfully!" });
        }

        [HttpPut("password")]
        [EndpointSummary("Change password")]
        [EndpointDescription("Change the owner's password. Requires the current password for verification. The new password must be at least 8 characters.")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request) {
            var userId = User.GetUserId();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null)
                return NotFound(new ApiErrorResponse { Message = "User not found." });

            var verify = _passwordHasher.VerifyHashedPassword(user, user.Password, request.CurrentPassword);
            if (verify == PasswordVerificationResult.Failed)
                return BadRequest(new ApiErrorResponse { Message = "Incorrect current password." });

            user.Password = _passwordHasher.HashPassword(user, request.NewPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse { Success = true, Message = "Password changed successfully!" });
        }

        [HttpPut("photo")]
        [Consumes("multipart/form-data")]
        [EndpointSummary("Update profile photo")]
        [EndpointDescription("Upload a new profile photo. The previous photo file is deleted from the server.")]
        [ProducesResponseType(typeof(ApiResponse<UpdatePhotoResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePhoto(IFormFile? photo) {
            var userId = User.GetUserId();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            var owner = await _context.Owners.FirstOrDefaultAsync(o => o.UserID == userId);

            if (user == null || owner == null)
                return NotFound(new ApiErrorResponse { Message = "Owner not found." });

            if (photo == null || photo.Length == 0)
                return BadRequest(new ApiErrorResponse { Message = "No file selected." });

            if (!string.IsNullOrEmpty(user.ProfileImage) && user.ProfileImage.StartsWith("/")) {
                var oldPath = Path.Combine("wwwroot", user.ProfileImage.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await photo.CopyToAsync(stream);

            var newPath = $"/uploads/profiles/{fileName}";
            user.ProfileImage = newPath;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<UpdatePhotoResponse> {
                Success = true,
                Message = "Profile photo updated successfully!",
                Data = new UpdatePhotoResponse { ProfileImageUrl = baseUrl + newPath }
            });
        }
    }
}
