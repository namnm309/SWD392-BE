using Application.DTOs.Notification;
using DomainLayer.Entities;
using DomainLayer.Enum;
using InfrastructureLayer.Core.Redis;
using InfrastructureLayer.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Notification
{
    public class NotificationService : INotificationService
    {
        private readonly LabDbContext _db;
        private readonly IRedisService _redis;

        public NotificationService(LabDbContext db, IRedisService redis)
        {
            _db = db;
            _redis = redis;
        }

        public async Task<IReadOnlyList<NotificationListItem>> GetAllNotificationsAsync(NotificationFilterRequest? filter = null)
        {
            var cacheKey = BuildNotificationListCacheKey(filter);
            var cached = await _redis.GetAsync<IReadOnlyList<NotificationListItem>>(cacheKey);
            if (cached != null) return cached;

            var query = _db.Notifications
                .Include(n => n.CreatedByUser)
                .AsQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.TargetGroup))
                    query = query.Where(n => n.TargetGroup == filter.TargetGroup);
                
                if (!string.IsNullOrEmpty(filter.Status))
                    query = query.Where(n => n.Status.ToString() == filter.Status);
                
                if (filter.StartDate.HasValue)
                    query = query.Where(n => n.StartDate >= filter.StartDate.Value);
                
                if (filter.EndDate.HasValue)
                    query = query.Where(n => n.EndDate <= filter.EndDate.Value);
            }

            query = query.OrderByDescending(n => n.CreatedAt);

            if (filter?.Page.HasValue == true && filter.PageSize.HasValue)
            {
                query = query.Skip(filter.Page.Value * filter.PageSize.Value)
                           .Take(filter.PageSize.Value);
            }

            var notifications = await query.ToListAsync();

            var result = notifications.Select(n => new NotificationListItem
            {
                Id = n.Id,
                Title = n.Title,
                Content = n.Content,
                TargetGroup = n.TargetGroup,
                StartDate = n.StartDate,
                EndDate = n.EndDate,
                Status = n.Status.ToString(),
                CreatedAt = n.CreatedAt,
                CreatedBy = n.CreatedByUser.Username,
                IsRead = false // Will be updated based on user context
            }).ToList();

            await _redis.SetAsync(cacheKey, result, RedisCacheDefaults.DefaultTtl);
            return result;
        }

        public async Task<NotificationDetail> GetNotificationByIdAsync(Guid id)
        {
            var cacheKey = RedisCacheKeyBuilder.Build("notifications:detail:v1", ("id", id));
            var cached = await _redis.GetAsync<NotificationDetail>(cacheKey);
            if (cached != null) return cached;

            var notification = await _db.Notifications
                .Include(n => n.CreatedByUser)
                .Include(n => n.NotificationReads)
                .FirstOrDefaultAsync(n => n.Id == id)
                ?? throw new Exception("Notification not found");

            var totalReaders = await _db.NotificationReads
                .CountAsync(nr => nr.NotificationId == id);

            var unreadCount = await GetUnreadCountForNotificationAsync(id);

            var detail = new NotificationDetail
            {
                Id = notification.Id,
                Title = notification.Title,
                Content = notification.Content,
                TargetGroup = notification.TargetGroup,
                StartDate = notification.StartDate,
                EndDate = notification.EndDate,
                Status = notification.Status.ToString(),
                CreatedAt = notification.CreatedAt,
                CreatedBy = notification.CreatedByUser.Username,
                CreatedByFullName = notification.CreatedByUser.Fullname,
                IsRead = false,
                TotalReaders = totalReaders,
                UnreadCount = unreadCount
            };

            await _redis.SetAsync(cacheKey, detail, RedisCacheDefaults.DefaultTtl);
            return detail;
        }

        public async Task<NotificationDetail> CreateNotificationAsync(CreateNotificationRequest request, Guid adminId)
        {
            var admin = await _db.Users.FirstOrDefaultAsync(u => u.Id == adminId)
                ?? throw new Exception("Admin not found");

            var notification = new DomainLayer.Entities.Notification
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Content = request.Content,
                TargetGroup = request.TargetGroup,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = NotificationStatus.Active,
                CreatedBy = adminId,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();

            await InvalidateNotificationCaches(notification.Id);
            return await GetNotificationByIdAsync(notification.Id);
        }

        public async Task<NotificationDetail> UpdateNotificationAsync(Guid id, UpdateNotificationRequest request, Guid adminId)
        {
            var notification = await _db.Notifications
                .FirstOrDefaultAsync(n => n.Id == id)
                ?? throw new Exception("Notification not found");

            if (!string.IsNullOrWhiteSpace(request.Title))
                notification.Title = request.Title;
            
            if (!string.IsNullOrWhiteSpace(request.Content))
                notification.Content = request.Content;
            
            if (!string.IsNullOrWhiteSpace(request.TargetGroup))
                notification.TargetGroup = request.TargetGroup;
            
            if (request.StartDate.HasValue)
                notification.StartDate = request.StartDate.Value;
            
            if (request.EndDate.HasValue)
                notification.EndDate = request.EndDate.Value;

            notification.LastUpdatedAt = DateTime.UtcNow;
            _db.Notifications.Update(notification);
            await _db.SaveChangesAsync();

            await InvalidateNotificationCaches(notification.Id);
            return await GetNotificationByIdAsync(notification.Id);
        }

        public async Task DeleteNotificationAsync(Guid id, Guid adminId)
        {
            var notification = await _db.Notifications
                .FirstOrDefaultAsync(n => n.Id == id)
                ?? throw new Exception("Notification not found");

            _db.Notifications.Remove(notification);
            await _db.SaveChangesAsync();

            await InvalidateNotificationCaches(notification.Id);
        }

        public async Task<IReadOnlyList<NotificationListItem>> GetUserNotificationsAsync(Guid userId, NotificationFilterRequest? filter = null)
        {
            var user = await _db.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new Exception("User not found");

            var userRoles = user.Roles.Select(r => r.name).ToList();
            var targetGroups = new List<string> { "All" };
            
            if (userRoles.Contains("Lecturer"))
                targetGroups.Add("Lecturer");
            if (userRoles.Contains("Student"))
                targetGroups.Add("Student");

            var cacheKey = BuildUserNotificationCacheKey(userId, filter);
            var cached = await _redis.GetAsync<IReadOnlyList<NotificationListItem>>(cacheKey);
            if (cached != null) return cached;

            var query = _db.Notifications
                .Include(n => n.CreatedByUser)
                .Include(n => n.NotificationReads.Where(nr => nr.UserId == userId))
                .Where(n => targetGroups.Contains(n.TargetGroup) && 
                           n.Status == NotificationStatus.Active &&
                           n.StartDate <= DateTime.UtcNow &&
                           n.EndDate >= DateTime.UtcNow)
                .AsQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.TargetGroup))
                    query = query.Where(n => n.TargetGroup == filter.TargetGroup);
                
                if (!string.IsNullOrEmpty(filter.Status))
                    query = query.Where(n => n.Status.ToString() == filter.Status);
                
                if (filter.StartDate.HasValue)
                    query = query.Where(n => n.StartDate >= filter.StartDate.Value);
                
                if (filter.EndDate.HasValue)
                    query = query.Where(n => n.EndDate <= filter.EndDate.Value);
            }

            query = query.OrderByDescending(n => n.CreatedAt);

            if (filter?.Page.HasValue == true && filter.PageSize.HasValue)
            {
                query = query.Skip(filter.Page.Value * filter.PageSize.Value)
                           .Take(filter.PageSize.Value);
            }

            var notifications = await query.ToListAsync();

            var result = notifications.Select(n => new NotificationListItem
            {
                Id = n.Id,
                Title = n.Title,
                Content = n.Content,
                TargetGroup = n.TargetGroup,
                StartDate = n.StartDate,
                EndDate = n.EndDate,
                Status = n.Status.ToString(),
                CreatedAt = n.CreatedAt,
                CreatedBy = n.CreatedByUser.Username,
                IsRead = n.NotificationReads.Any()
            }).ToList();

            await _redis.SetAsync(cacheKey, result, RedisCacheDefaults.DefaultTtl);
            return result;
        }

        public async Task MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            var existingRead = await _db.NotificationReads
                .FirstOrDefaultAsync(nr => nr.NotificationId == notificationId && nr.UserId == userId);

            if (existingRead == null)
            {
                var notificationRead = new NotificationRead
                {
                    Id = Guid.NewGuid(),
                    NotificationId = notificationId,
                    UserId = userId,
                    ReadAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow
                };

                _db.NotificationReads.Add(notificationRead);
                await _db.SaveChangesAsync();

                await InvalidateNotificationUserCaches(userId);
                await InvalidateNotificationCaches(notificationId);
            }
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            var user = await _db.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new Exception("User not found");

            var userRoles = user.Roles.Select(r => r.name).ToList();
            var targetGroups = new List<string> { "All" };
            
            if (userRoles.Contains("Lecturer"))
                targetGroups.Add("Lecturer");
            if (userRoles.Contains("Student"))
                targetGroups.Add("Student");

            var unreadNotifications = await _db.Notifications
                .Where(n => targetGroups.Contains(n.TargetGroup) && 
                           n.Status == NotificationStatus.Active &&
                           n.StartDate <= DateTime.UtcNow &&
                           n.EndDate >= DateTime.UtcNow &&
                           !n.NotificationReads.Any(nr => nr.UserId == userId))
                .Select(n => n.Id)
                .ToListAsync();

            var notificationReads = unreadNotifications.Select(notificationId => new NotificationRead
            {
                Id = Guid.NewGuid(),
                NotificationId = notificationId,
                UserId = userId,
                ReadAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            }).ToList();

            _db.NotificationReads.AddRange(notificationReads);
            await _db.SaveChangesAsync();

            await InvalidateNotificationUserCaches(userId);
            foreach (var notificationId in unreadNotifications)
            {
                await InvalidateNotificationCaches(notificationId);
            }
        }

        public async Task UpdateNotificationStatusAsync()
        {
            var now = DateTime.UtcNow;
            
            // Mark expired notifications
            var expiredNotifications = await _db.Notifications
                .Where(n => n.Status == NotificationStatus.Active && n.EndDate < now)
                .ToListAsync();

            foreach (var notification in expiredNotifications)
            {
                notification.Status = NotificationStatus.Expired;
                notification.LastUpdatedAt = now;
            }

            _db.Notifications.UpdateRange(expiredNotifications);
            await _db.SaveChangesAsync();

            foreach (var notification in expiredNotifications)
            {
                await InvalidateNotificationCaches(notification.Id);
            }
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            var user = await _db.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new Exception("User not found");

            var userRoles = user.Roles.Select(r => r.name).ToList();
            var targetGroups = new List<string> { "All" };
            
            if (userRoles.Contains("Lecturer"))
                targetGroups.Add("Lecturer");
            if (userRoles.Contains("Student"))
                targetGroups.Add("Student");

            var cacheKey = BuildUnreadCountCacheKey(userId);
            var cached = await _redis.GetAsync<int?>(cacheKey);
            if (cached.HasValue) return cached.Value;

            var count = await _db.Notifications
                .Where(n => targetGroups.Contains(n.TargetGroup) && 
                           n.Status == NotificationStatus.Active &&
                           n.StartDate <= DateTime.UtcNow &&
                           n.EndDate >= DateTime.UtcNow &&
                           !n.NotificationReads.Any(nr => nr.UserId == userId))
                .CountAsync();

            await _redis.SetAsync(cacheKey, count, RedisCacheDefaults.DefaultTtl);
            return count;
        }

        private static string BuildNotificationListCacheKey(NotificationFilterRequest? filter)
        {
            if (filter == null)
            {
                return "notifications:list:v1";
            }

            return RedisCacheKeyBuilder.Build(
                "notifications:list:v1",
                ("target", filter.TargetGroup),
                ("status", filter.Status),
                ("start", filter.StartDate),
                ("end", filter.EndDate),
                ("page", filter.Page),
                ("pageSize", filter.PageSize));
        }

        private static string BuildUserNotificationCacheKey(Guid userId, NotificationFilterRequest? filter)
        {
            if (filter == null)
            {
                return RedisCacheKeyBuilder.Build("notifications:user-list:v1", ("userId", userId));
            }

            return RedisCacheKeyBuilder.Build(
                "notifications:user-list:v1",
                ("userId", userId),
                ("target", filter.TargetGroup),
                ("status", filter.Status),
                ("start", filter.StartDate),
                ("end", filter.EndDate),
                ("page", filter.Page),
                ("pageSize", filter.PageSize));
        }

        private static string BuildUnreadCountCacheKey(Guid userId)
        {
            return RedisCacheKeyBuilder.Build("notifications:unread-count:v1", ("userId", userId));
        }

        private async Task InvalidateNotificationCaches(Guid notificationId)
        {
            await _redis.RemoveAsync("notifications:list:v1");
            await _redis.RemoveAsync(RedisCacheKeyBuilder.Build("notifications:detail:v1", ("id", notificationId)));
        }

        private async Task InvalidateNotificationUserCaches(Guid userId)
        {
            await _redis.RemoveAsync(RedisCacheKeyBuilder.Build("notifications:user-list:v1", ("userId", userId)));
            await _redis.RemoveAsync(RedisCacheKeyBuilder.Build("notifications:unread-count:v1", ("userId", userId)));
        }

        private async Task<int> GetUnreadCountForNotificationAsync(Guid notificationId)
        {
            var notification = await _db.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId)
                ?? throw new Exception("Notification not found");

            var userRoles = new List<string>();
            if (notification.TargetGroup == "All")
            {
                userRoles = await _db.Users
                    .SelectMany(u => u.Roles)
                    .Select(r => r.name)
                    .Distinct()
                    .ToListAsync();
            }
            else
            {
                userRoles.Add(notification.TargetGroup);
            }

            var totalUsers = await _db.Users
                .Where(u => u.Roles.Any(r => userRoles.Contains(r.name)))
                .CountAsync();

            var readCount = await _db.NotificationReads
                .CountAsync(nr => nr.NotificationId == notificationId);

            return totalUsers - readCount;
        }
    }
}
