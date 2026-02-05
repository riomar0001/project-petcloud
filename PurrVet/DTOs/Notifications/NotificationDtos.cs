namespace PurrVet.DTOs.Notifications {
    public class NotificationDto {
        public int NotificationId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string? RedirectUrl { get; set; }
    }

    public class UnreadCountResponse {
        public int UnreadCount { get; set; }
    }
}
