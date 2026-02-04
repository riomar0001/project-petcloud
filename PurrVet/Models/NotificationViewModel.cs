namespace PurrVet.Models {
    public class NotificationViewModel {

        public int NotificationID { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string RedirectUrl { get; set; }
    }

    public class NotificationListViewModel {
        public List<NotificationViewModel> Notifications { get; set; }
        public string TypeFilter { get; set; }
        public string StatusFilter { get; set; }
        public string SearchQuery { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
