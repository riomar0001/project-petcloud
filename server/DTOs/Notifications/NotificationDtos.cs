using PetCloud.DTOs.Common;

namespace PetCloud.DTOs.Notifications {
    public class NotificationDto {
        public int NotificationId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string? RedirectUrl { get; set; }
    }

    public class NotificationListResponse : PaginatedResponse<NotificationDto> {
        public int UnreadCount { get; set; }
    }

    public class UnreadCountResponse {
        public int UnreadCount { get; set; }
    }
}
