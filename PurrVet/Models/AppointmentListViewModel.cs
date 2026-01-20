namespace PurrVet.Models
{
    public class AppointmentListViewModel
    {
        public List<AppointmentGroupViewModel> AppointmentGroups { get; set; } = new List<AppointmentGroupViewModel>();
        public IEnumerable<Appointment> Appointments { get; set; } = new List<Appointment>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string SearchQuery { get; set; } = "";
        public string StatusFilter { get; set; } = "";
    }
}
