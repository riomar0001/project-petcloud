using System;
using System.ComponentModel.DataAnnotations;

namespace PurrVet.Models
{
    public class SystemLog
    {
        [Key]
        public int LogID { get; set; }

        [Required]
        [MaxLength(100)]
        public string ActionType { get; set; }

        [MaxLength(255)]
        public string PerformedBy { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } 

        [MaxLength(50)]
        public string Module { get; set; } 
    }
}
