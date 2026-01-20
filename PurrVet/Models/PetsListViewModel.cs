namespace PurrVet.Models
{
    public class PetsListViewModel
    {
        public IEnumerable<Pet> Pets { get; set; } = new List<Pet>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string SearchQuery { get; set; } = "";
        public string TypeFilter { get; set; } = "";
    }
}
