using System.ComponentModel.DataAnnotations;

namespace PurrVet.Models {
    public class AppointmentBulkForm {
        [Required]
        public List<AppointmentInput> Appointments { get; set; } = new();
    }

    public class AppointmentInput {
        [Required(ErrorMessage = "Pet selection is required.")]
        public int PetID { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        public int? CategoryID { get; set; }

        [Required(ErrorMessage = "Subtype is required.")]
        public int? SubtypeID { get; set; }

        [Required(ErrorMessage = "Date is required.")]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Time is required.")]
        public string AppointmentTime { get; set; } = string.Empty;

        [Required(ErrorMessage = "Notes are required.")]
        [MinLength(2, ErrorMessage = "Notes must contain at least 2 characters.")]
        public string? Notes { get; set; }
    }
}
