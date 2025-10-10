using Application.DTOs.Notification;
using DomainLayer.Enum;

namespace Application.Services.Notification
{
    public interface INotificationService
    {
        // Admin functions
        Task<IReadOnlyList<NotificationListItem>> GetAllNotificationsAsync(NotificationFilterRequest? filter = null);
        Task<NotificationDetail> GetNotificationByIdAsync(Guid id);
        Task<NotificationDetail> CreateNotificationAsync(CreateNotificationRequest request, Guid adminId);
        Task<NotificationDetail> UpdateNotificationAsync(Guid id, UpdateNotificationRequest request, Guid adminId);
        Task DeleteNotificationAsync(Guid id, Guid adminId);
        
        // User functions
        Task<IReadOnlyList<NotificationListItem>> GetUserNotificationsAsync(Guid userId, NotificationFilterRequest? filter = null);
        Task MarkAsReadAsync(Guid notificationId, Guid userId);
        Task MarkAllAsReadAsync(Guid userId);
        
        // Utility functions
        Task UpdateNotificationStatusAsync();
        Task<int> GetUnreadCountAsync(Guid userId);
    }
}
