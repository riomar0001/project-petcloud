namespace PurrVet.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Collections.Generic;

    public class Pet
    {
        [Key]
        public int PetID { get; set; }

        [ForeignKey("Owner")]
        public int OwnerID { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required, StringLength(100)]
        public string Type { get; set; }

        [Required, StringLength(100)]
        public string Breed { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime Birthdate { get; set; }
        public string? PhotoPath { get; set; }
        public Owner Owner { get; set; }
        public ICollection<Appointment> Appointments { get; set; }
    }
}
