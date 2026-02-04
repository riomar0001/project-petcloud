namespace PurrVet.Models {
    public class AppointmentGroupViewModel {
        public int GroupID { get; set; }
        public List<Appointment> Appointments { get; set; } = new();
        public int AppointmentCount => Appointments?.Count ?? 0;
        public DateTime GroupTime => Appointments?.Min(a => a.AppointmentDate) ?? DateTime.MinValue;
        public string PetNames => string.Join(", ", Appointments.Select(a => a.Pet?.Name).Distinct());
        public string Owners => string.Join(", ", Appointments.Select(a => a.Pet?.Owner?.Name).Distinct());
        public string Status => string.Join(", ", Appointments.Select(a => a.Status).Distinct());
        public string Notes => string.Join("; ", Appointments.Select(a => a.Notes).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct());
    }
}
