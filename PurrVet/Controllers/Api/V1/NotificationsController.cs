using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PurrVet.DTOs.Common;
using PurrVet.DTOs.Notifications;
using PurrVet.Infrastructure;
using PurrVet.Models;

namespace PurrVet.Controllers.Api.V1 {
    [ApiController]
    [Route("api/v1/notifications")]
    [Authorize(Policy = "OwnerOnly")]
    public class NotificationsController : ControllerBase {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context) {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetNotifications(
            [FromQuery] string typeFilter = "All",
            [FromQuery] string statusFilter = "All",
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10) {
            var userId = User.GetUserId();

            var query = _context.Notifications
                .Where(n => n.TargetUserId == userId || n.TargetRole == "Owner");

            if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "All")
                query = query.Where(n => n.Type == typeFilter);

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All") {
                if (statusFilter == "Read") query = query.Where(n => n.IsRead);
                else if (statusFilter == "Unread") query = query.Where(n => !n.IsRead);
            }

            if (!string.IsNullOrEmpty(search))
                query = query.Where(n => n.Message.Contains(search));

            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var unreadCount = _context.Notifications
                .Count(n => (n.TargetUserId == userId || n.TargetRole == "Owner") && !n.IsRead);

            var notifications = query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationDto {
                    NotificationId = n.NotificationID,
                    Message = n.Message,
                    Type = n.Type,
                    CreatedAt = n.CreatedAt,
                    IsRead = n.IsRead,
                    RedirectUrl = n.RedirectUrl
                }).ToList();

            return Ok(new {
                success = true,
                items = notifications,
                currentPage = page,
                totalPages,
                totalCount,
                pageSize,
                unreadCount,
                hasPrevious = page > 1,
                hasNext = page < totalPages
            });
        }

        [HttpGet("unread-count")]
        public IActionResult GetUnreadCount() {
            var userId = User.GetUserId();

            var count = _context.Notifications
                .Count(n => (n.TargetUserId == userId || n.TargetRole == "Owner") && !n.IsRead);

            return Ok(new ApiResponse<UnreadCountResponse> {
                Success = true,
                Data = new UnreadCountResponse { UnreadCount = count }
            });
        }

        [HttpPut("{id}/read")]
        public IActionResult MarkAsRead(int id) {
            var userId = User.GetUserId();

            var notif = _context.Notifications
                .FirstOrDefault(n => n.NotificationID == id &&
                    (n.TargetUserId == userId || n.TargetRole == "Owner"));

            if (notif == null)
                return NotFound(new ApiErrorResponse { Message = "Notification not found." });

            notif.IsRead = true;
            _context.SaveChanges();

            return Ok(new ApiResponse { Success = true, Message = "Notification marked as read." });
        }

        [HttpPut("read-all")]
        public IActionResult MarkAllAsRead() {
            var userId = User.GetUserId();

            var unread = _context.Notifications
                .Where(n => (n.TargetUserId == userId || n.TargetRole == "Owner") && !n.IsRead)
                .ToList();

            if (!unread.Any())
                return Ok(new ApiResponse { Success = true, Message = "No unread notifications." });

            foreach (var n in unread)
                n.IsRead = true;

            _context.SaveChanges();

            return Ok(new ApiResponse { Success = true, Message = $"Marked {unread.Count} notification(s) as read." });
        }
    }
}
