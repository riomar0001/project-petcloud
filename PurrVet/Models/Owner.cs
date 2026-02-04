namespace PurrVet.Models {
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Owner {
        [Key]
        public int OwnerID { get; set; }

        [ForeignKey("User")]
        public int UserID { get; set; }

        [Required, StringLength(50)]
        public string Name { get; set; }

        [Required, StringLength(50)]
        public string Email { get; set; }

        [StringLength(12)]
        public string Phone { get; set; }

        public User User { get; set; }
        public ICollection<Pet> Pets { get; set; }
    }
}
