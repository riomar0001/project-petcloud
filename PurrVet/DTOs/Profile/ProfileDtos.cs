using System.ComponentModel.DataAnnotations;

namespace PurrVet.DTOs.Profile {
    public class ProfileResponse {
        public int UserId { get; set; }
        public int OwnerId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? ProfileImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateProfileRequest {
        [Required, StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required, RegularExpression(@"^\d{11}$", ErrorMessage = "Phone number must be exactly 11 digits.")]
        public string Phone { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required, Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
