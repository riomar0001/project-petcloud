using System.ComponentModel.DataAnnotations;

namespace PurrVet.Models {
    public class AppointmentGroup {
        [Key]
        public int GroupID { get; set; }

        [Required]
        public DateTime GroupTime { get; set; }

        [Required(ErrorMessage = "Group notes are required.")]
        [MinLength(2, ErrorMessage = "Notes must" +
            "contain at least 2 characters.")]
        public string? Notes { get; set; }
        public string Status { get; set; } = "Draft";
        public DateTime? FinalizedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
