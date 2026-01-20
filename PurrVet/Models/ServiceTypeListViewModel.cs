namespace PurrVet.Models
{
    public class ServiceTypeListViewModel
    {
        public List<ServiceSubtype> ServiceSubtypes { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string SearchQuery { get; set; }
        public int? CategoryFilter { get; set; }
    }
}
