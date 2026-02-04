namespace PurrVet.Models {
    public class DewormingListViewModel {
        public IEnumerable<Appointment> Deworming { get; set; } = new List<Appointment>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string SearchQuery { get; set; } = "";
        public string StatusFilter { get; set; } = "";
        public int TotalRecords { get; set; }
        public string CategoryName { get; set; } = "";
        public List<string> ServiceSubtypes { get; set; } = new List<string>();
        public string SubtypeFilter { get; set; } = "";
    }
}
