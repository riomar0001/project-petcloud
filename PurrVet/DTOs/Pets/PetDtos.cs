using System.ComponentModel.DataAnnotations;

namespace PurrVet.DTOs.Pets {
    public class PetListItemDto {
        public int PetId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Breed { get; set; } = string.Empty;
        public DateTime Birthdate { get; set; }
        public string? PhotoUrl { get; set; }
        public string Age { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class PetDetailDto {
        public int PetId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Breed { get; set; } = string.Empty;
        public DateTime Birthdate { get; set; }
        public string? PhotoUrl { get; set; }
        public string Age { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
    }

    public class CreatePetRequest {
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Type { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Breed { get; set; }

        [Required]
        public DateTime Birthdate { get; set; }

        public IFormFile? Photo { get; set; }
    }

    public class UpdatePetRequest {
        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(100)]
        public string? Type { get; set; }

        [StringLength(100)]
        public string? Breed { get; set; }

        public DateTime? Birthdate { get; set; }

        public IFormFile? Photo { get; set; }
    }
}
