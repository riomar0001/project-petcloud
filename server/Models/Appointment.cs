namespace PetCloud.Models {
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Appointment {
        [Key]
        public int AppointmentID { get; set; }

        [ForeignKey("Pet")]
        public int PetID { get; set; }
        [ForeignKey("AppointmentGroup")]
        public int? GroupID { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }
        public DateTime? DueDate { get; set; }

        [ForeignKey("ServiceCategory")]
        public int? CategoryID { get; set; }

        [ForeignKey("ServiceSubtype")]
        public int? SubtypeID { get; set; }

        public string Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? AdministeredBy { get; set; }
        public string? Notes { get; set; } = "No notes.";
        public bool IsSynced { get; set; } = false;

        public DateTime? LastSmsSentAt { get; set; }
        public DateTime? LastEmailSentAt { get; set; }
        public int SmsSentToday { get; set; }
        public int EmailSentToday { get; set; }
        public DateTime? ReminderCounterDate { get; set; }

        public Pet Pet { get; set; }
        public AppointmentGroup? AppointmentGroup { get; set; }
        public ServiceCategory? ServiceCategory { get; set; }
        public ServiceSubtype? ServiceSubtype { get; set; }
    }
}
