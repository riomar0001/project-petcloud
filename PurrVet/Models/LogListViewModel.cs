namespace PurrVet.Models {
    public class LogListViewModel {
        public IEnumerable<SystemLog> Logs { get; set; } = new List<SystemLog>();

        public string TypeFilter { get; set; }
        public string ModuleFilter { get; set; }
        public string SearchQuery { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
    }
}
