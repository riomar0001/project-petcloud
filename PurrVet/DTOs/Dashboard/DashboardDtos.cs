namespace PurrVet.DTOs.Dashboard {
    public class DashboardResponse {
        public string UserName { get; set; } = string.Empty;
        public List<DashboardPetDto> Pets { get; set; } = new();
        public List<DashboardAppointmentDto> UpcomingAppointments { get; set; } = new();
        public List<DashboardDueItemDto> VaccineDue { get; set; } = new();
        public List<DashboardDueItemDto> DewormDue { get; set; } = new();
    }

    public class DashboardPetDto {
        public int PetId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Breed { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public DateTime Birthdate { get; set; }
    }

    public class DashboardAppointmentDto {
        public int AppointmentId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int PetId { get; set; }
        public string PetName { get; set; } = string.Empty;
        public string? ServiceType { get; set; }
    }

    public class DashboardDueItemDto {
        public int AppointmentId { get; set; }
        public DateTime? DueDate { get; set; }
        public int PetId { get; set; }
        public string PetName { get; set; } = string.Empty;
        public string? ServiceType { get; set; }
    }
}
