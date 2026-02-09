using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetCloud.Models {
    public class RefreshToken {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserID { get; set; }

        [Required, MaxLength(500)]
        public string Token { get; set; }

        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAt { get; set; }

        [MaxLength(500)]
        public string? ReplacedByToken { get; set; }

        [MaxLength(200)]
        public string? DeviceInfo { get; set; }

        public bool IsRevoked => RevokedAt != null;
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsActive => !IsRevoked && !IsExpired;

        public User User { get; set; }
    }
}
