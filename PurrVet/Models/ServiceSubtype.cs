using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PurrVet.Models
{
    public class ServiceSubtype
    {
        [Key]
        public int SubtypeID { get; set; }

        [ForeignKey("ServiceCategory")]
        public int CategoryID { get; set; }

        [Required]
        [MaxLength(100)]
        public string ServiceSubType { get; set; } 

        public string? Description { get; set; }

        public ServiceCategory ServiceCategory { get; set; }
    }
}
