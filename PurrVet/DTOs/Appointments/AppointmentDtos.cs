using System.ComponentModel.DataAnnotations;

namespace PurrVet.DTOs.Appointments {
    public class AppointmentListItemDto {
        public int AppointmentId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? GroupId { get; set; }
        public string? Notes { get; set; }
        public int PetId { get; set; }
        public string PetName { get; set; } = string.Empty;
        public string? ServiceType { get; set; }
        public string? ServiceSubtype { get; set; }
        public bool IsOwnAppointment { get; set; }
    }

    public class CreateAppointmentRequest {
        [Required]
        public int PetId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public int? SubtypeId { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public string AppointmentTime { get; set; } = string.Empty;

        public string? Notes { get; set; }
    }

    public class CreateBulkAppointmentRequest {
        [Required, MinLength(1)]
        public List<BulkAppointmentItemRequest> Appointments { get; set; } = new();
    }

    public class BulkAppointmentItemRequest {
        [Required]
        public int PetId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public int? SubtypeId { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public string AppointmentTime { get; set; } = string.Empty;

        public string? Notes { get; set; }
    }

    public class BulkAppointmentResponse {
        public int GroupId { get; set; }
        public int Count { get; set; }
    }

    public class TimeSlotDto {
        public string Time { get; set; } = string.Empty;
        public bool Available { get; set; }
    }

    public class TimeSlotsResponse {
        public string Date { get; set; } = string.Empty;
        public List<TimeSlotDto> Slots { get; set; } = new();
    }

    public class ServiceCategoryDto {
        public int CategoryId { get; set; }
        public string ServiceType { get; set; } = string.Empty;
        public List<ServiceSubtypeDto> Subtypes { get; set; } = new();
    }

    public class ServiceSubtypeDto {
        public int SubtypeId { get; set; }
        public string ServiceSubType { get; set; } = string.Empty;
    }
}
