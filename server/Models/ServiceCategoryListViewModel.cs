namespace PetCloud.Models {
    public class ServiceCategoryListViewModel {
        public List<ServiceCategory> ServiceCategories { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string SearchQuery { get; set; }
    }
}
