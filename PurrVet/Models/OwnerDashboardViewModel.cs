namespace PurrVet.Models {
    public class OwnerDashboardViewModel {
        public string UserName { get; set; }
        public List<dynamic> Pets { get; set; } = new();
        public List<dynamic> UpcomingAppointments { get; set; } = new();
        public List<dynamic> VaccineDue { get; set; } = new();
        public List<dynamic> DewormDue { get; set; } = new();
    }

}
