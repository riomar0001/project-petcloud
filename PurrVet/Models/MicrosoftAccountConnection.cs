using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PurrVet.Models
{
    public class MicrosoftAccountConnection
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(User))] 
        public int UserID { get; set; }

        [Required, MaxLength(255)]
        public string MicrosoftEmail { get; set; }

        [MaxLength(2000)]
        public string? AccessToken { get; set; }

        [MaxLength(2000)]
        public string? RefreshToken { get; set; }

        public DateTime? TokenExpiry { get; set; }

        public DateTime ConnectedAt { get; set; } = DateTime.Now;
        public bool IsAutoSyncEnabled { get; set; } = false;

        public User User { get; set; }
    }
}
