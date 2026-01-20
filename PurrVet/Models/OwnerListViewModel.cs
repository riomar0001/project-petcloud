namespace PurrVet.Models
{
    public class OwnerListViewModel
    {
        public IEnumerable<Owner> Owners { get; set; } = new List<Owner>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string SearchQuery { get; set; } = "";
        public string StatusFilter { get; set; } = "";
    }
}
