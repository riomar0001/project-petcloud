namespace PetCloud.Models {
    using System;
    using System.ComponentModel.DataAnnotations;

    public class Notification {
        [Key]
        public int NotificationID { get; set; }

        [Required]
        [StringLength(255)]
        public string Message { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;
        public string? RedirectUrl { get; set; }
        public string? TargetRole { get; set; }
        public int? TargetUserId { get; set; }
    }
}
