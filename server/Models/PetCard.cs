namespace PetCloud.Models {
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class PetCard {
        [Key]
        public int PetCardID { get; set; }

        [ForeignKey("Appointment")]
        public int AppointmentID { get; set; }
        public DateTime DateAdministered { get; set; }
        public DateTime? NextDueDate { get; set; }
        public Appointment Appointment { get; set; }
    }

}
