namespace PetCloud.Models {
    public class UserListViewModel {
        public IEnumerable<User> Users { get; set; } = new List<User>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string SearchQuery { get; set; } = "";
        public string StatusFilter { get; set; } = "";
    }

}
