using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetCloud.Models {
    public class AppointmentDraft {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DraftID { get; set; }

        public int? UserID { get; set; }
        public int? OwnerID { get; set; }

        [ForeignKey("Pet")]
        public int? PetID { get; set; }

        [ForeignKey("ServiceCategory")]
        public int? CategoryID { get; set; }

        [ForeignKey("ServiceSubtype")]
        public int? SubtypeID { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        public string? AppointmentTime { get; set; }
        public string? GroupDraftId { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Pet? Pet { get; set; }
        public ServiceCategory? ServiceCategory { get; set; }
        public ServiceSubtype? ServiceSubtype { get; set; }
    }
}
