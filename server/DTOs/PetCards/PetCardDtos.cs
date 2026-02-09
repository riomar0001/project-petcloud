using PetCloud.DTOs.Pets;

namespace PetCloud.DTOs.PetCards {
    public class PetCardResponse {
        public PetListItemDto Pet { get; set; } = new();
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerPhone { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public int AgeInMonths { get; set; }
        public List<PetCardRecordDto> Records { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
    }

    public class PetCardRecordDto {
        public int AppointmentId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string? Notes { get; set; }
        public string? ServiceType { get; set; }
        public string? ServiceSubtype { get; set; }
        public string? AdministeredBy { get; set; }
        public DateTime? DueDate { get; set; }
    }
}
