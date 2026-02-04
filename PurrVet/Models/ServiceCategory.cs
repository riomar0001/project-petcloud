using System.ComponentModel.DataAnnotations;

namespace PurrVet.Models {
    public class ServiceCategory {
        [Key]
        public int CategoryID { get; set; }

        [Required]
        [MaxLength(100)]
        public string ServiceType { get; set; }

        public string? Description { get; set; }

        public ICollection<ServiceSubtype>? Subtypes { get; set; }
    }
}
