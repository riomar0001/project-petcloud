using System.ComponentModel.DataAnnotations;

namespace PetCloud.DTOs.Auth {
    public class LoginRequest {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public string? DeviceInfo { get; set; }
    }

    public class LoginResponse {
        public bool Requires2FA { get; set; }
        public int? TwoFactorUserId { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public OwnerBasicDto? Owner { get; set; }
    }

    public class OwnerBasicDto {
        public int UserId { get; set; }
        public int OwnerId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
    }

    public class Verify2FARequest {
        [Required]
        public int UserId { get; set; }

        [Required, StringLength(6, MinimumLength = 6)]
        public string Code { get; set; } = string.Empty;

        public string? DeviceInfo { get; set; }
    }

    public class TokenResponse {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public OwnerBasicDto? Owner { get; set; }
    }

    public class RefreshTokenRequest {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class RegisterRequest {
        [Required, StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, RegularExpression(@"^\d{11}$", ErrorMessage = "Phone number must be exactly 11 digits.")]
        public string Phone { get; set; } = string.Empty;

        [Required, MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        public string Password { get; set; } = string.Empty;

        [Required, Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequest {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required, MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required, Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
