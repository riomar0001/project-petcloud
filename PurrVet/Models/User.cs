namespace PurrVet.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required, StringLength(50)]
        public string FirstName { get; set; }

        [Required, StringLength(50)]
        public string LastName { get; set; }

        [Required, StringLength(50)]
        public string Email { get; set; }

        [StringLength(12)]
        public string Phone { get; set; }

        [Required]
        public string Password { get; set; }

        [StringLength(20)]
        public string Type { get; set; }

        [StringLength(20)]
        public string Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string ProfileImage { get; set; }
        public string? ResetToken { get; set; }
        public DateTime? TokenExpiry { get; set; }
        public bool TwoFactorEnabled { get; set; } = true; 
        public string? TwoFactorCode { get; set; } 
        public DateTime? TwoFactorExpiry { get; set; } 
        public DateTime? LastTwoFactorVerification { get; set; }
        public string? LastLoginIP { get; set; }
        public string? LastLoginDevice { get; set; }

        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEnd { get; set; }

        public Owner Owner { get; set; }

    }
}