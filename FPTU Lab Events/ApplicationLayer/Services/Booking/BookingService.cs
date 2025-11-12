using Application.DTOs.Booking;
using DomainLayer.Entities;
using DomainLayer.Enum;
using InfrastructureLayer.Data;
using InfrastructureLayer.Core.Redis;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Booking
{
	public class BookingService : IBookingService
	{
        private readonly LabDbContext _db;
        private readonly IRedisService _redis;

        public BookingService(LabDbContext db, IRedisService redis)
        {
            _db = db;
            _redis = redis;
        }

        public async Task<IReadOnlyList<BookingListItem>> GetBookingsAsync(BookingFilterRequest? filter = null)
		{
            var cacheKey = BuildListCacheKey(filter);
            var cached = await _redis.GetAsync<IReadOnlyList<BookingListItem>>(cacheKey);
            if (cached != null) return cached;

			var query = _db.Bookings
				.Include(b => b.Room)
				.Include(b => b.User)
				.AsQueryable();

			if (filter != null)
			{
				if (filter.RoomId.HasValue) query = query.Where(b => b.RoomId == filter.RoomId.Value);
				if (filter.UserId.HasValue) query = query.Where(b => b.UserId == filter.UserId.Value);
				if (filter.EventId.HasValue) query = query.Where(b => b.EventId == filter.EventId.Value);
				if (filter.Status.HasValue) query = query.Where(b => b.Status == filter.Status.Value);
				if (filter.From.HasValue) query = query.Where(b => b.EndTime >= filter.From.Value);
				if (filter.To.HasValue) query = query.Where(b => b.StartTime <= filter.To.Value);
			}

			query = query.OrderByDescending(b => b.StartTime);

			if (filter?.Page.HasValue == true && filter.PageSize.HasValue)
			{
				query = query.Skip(filter.Page.Value * filter.PageSize.Value)
						   .Take(filter.PageSize.Value);
			}

            var items = await query.ToListAsync();
            var result = items.Select(b => new BookingListItem
			{
				Id = b.Id,
				RoomId = b.RoomId,
				RoomName = b.Room.Name,
				UserId = b.UserId,
				UserName = b.User.Fullname,
				StartTime = b.StartTime,
				EndTime = b.EndTime,
				Status = b.Status,
				EventId = b.EventId,
				Purpose = b.Purpose
            }).ToList();

            await _redis.SetAsync(cacheKey, result, RedisCacheDefaults.DefaultTtl);
            return result;
		}

		public async Task<BookingDetail> GetByIdAsync(Guid id)
		{
            var cacheKey = RedisCacheKeyBuilder.Build("bookings:detail:v1", ("id", id));
            var cached = await _redis.GetAsync<BookingDetail>(cacheKey);
            if (cached != null) return cached;

			var b = await _db.Bookings
				.Include(x => x.User)
				.Include(x => x.Room)
				.FirstOrDefaultAsync(x => x.Id == id)
				?? throw new Exception("Booking not found");

			var detail = new BookingDetail
			{
				Id = b.Id,
				RoomId = b.RoomId,
				RoomName = b.Room.Name,
				UserId = b.UserId,
				UserName = b.User.Fullname,
				StartTime = b.StartTime,
				EndTime = b.EndTime,
				Status = b.Status,
				EventId = b.EventId,
				Purpose = b.Purpose,
				Notes = b.Notes
			};

            await _redis.SetAsync(cacheKey, detail, RedisCacheDefaults.DefaultTtl);
            return detail;
		}

		public async Task<IReadOnlyList<BookingListItem>> GetBookingsByUserIdAsync(Guid userId, int? page = null, int? pageSize = null)
		{
            var cacheKey = RedisCacheKeyBuilder.Build(
                "bookings:user:v1",
                ("userId", userId),
                ("page", page),
                ("pageSize", pageSize));

            var cached = await _redis.GetAsync<IReadOnlyList<BookingListItem>>(cacheKey);
            if (cached != null) return cached;

			var baseQuery = _db.Bookings
				.Include(b => b.Room)
				.Include(b => b.User)
				.Where(b => b.UserId == userId)
				.OrderByDescending(b => b.StartTime);

			// Apply pagination if provided
			var query = page.HasValue && pageSize.HasValue
				? baseQuery.Skip(page.Value * pageSize.Value).Take(pageSize.Value)
				: baseQuery;

			var bookings = await query.ToListAsync();

			var result = bookings.Select(b => new BookingListItem
			{
				Id = b.Id,
				RoomId = b.RoomId,
				RoomName = b.Room.Name,
				UserId = b.UserId,
				UserName = b.User.Fullname,
				StartTime = b.StartTime,
				EndTime = b.EndTime,
				Status = b.Status,
				EventId = b.EventId,
				Purpose = b.Purpose
			}).ToList();

            await _redis.SetAsync(cacheKey, result, RedisCacheDefaults.DefaultTtl);
            return result;
		}

		public async Task<BookingDetail> CreateAsync(Guid currentUserId, CreateBookingRequest request)
		{
			// Validate EventId is required
			if (request.EventId == Guid.Empty)
				throw new Exception("EventId is required");

			// Get Event with RoomSlots
			var eventEntity = await _db.Events
				.Include(e => e.RoomSlots)
					.ThenInclude(rs => rs.Room)
				.Include(e => e.Bookings.Where(b => b.Status == BookingStatus.Approved))
				.FirstOrDefaultAsync(e => e.Id == request.EventId)
				?? throw new Exception("Event not found");

			// Validate Event status - allow booking for Active and Inactive events
			// Only block Cancelled and Completed events
			if (eventEntity.Status == DomainLayer.Enum.EventStatus.Cancelled)
				throw new Exception("Event has been cancelled and cannot accept bookings");
			
			if (eventEntity.Status == DomainLayer.Enum.EventStatus.Completed)
				throw new Exception("Event has been completed and cannot accept bookings");

			// Validate Event has RoomSlots
			if (!eventEntity.RoomSlots.Any())
				throw new Exception("Event does not have any RoomSlots assigned");

			// Get RoomId from first RoomSlot (all slots should belong to same room)
			var firstSlot = eventEntity.RoomSlots.First();
			var roomId = firstSlot.RoomId;
			var room = firstSlot.Room;

			// Validate Room is available
			if (room.Status != RoomStatus.Available)
				throw new Exception($"Room '{room.Name}' is not available");

			// Check if user already has a booking for this event
			var existingBooking = await _db.Bookings
				.AnyAsync(b => b.UserId == currentUserId && 
							   b.EventId == request.EventId && 
							   b.Status != BookingStatus.Rejected && 
							   b.Status != BookingStatus.Cancelled);
			if (existingBooking)
				throw new Exception("You already have a booking for this event");

			// Check Room capacity for this event
			// Count approved bookings for this event in this room
			var approvedBookingsCount = await _db.Bookings
				.CountAsync(b => b.EventId == request.EventId && 
								 b.RoomId == roomId && 
								 b.Status == BookingStatus.Approved);
			
			if (room.Capacity > 0 && approvedBookingsCount >= room.Capacity)
				throw new Exception($"Room '{room.Name}' has reached its capacity ({room.Capacity}) for this event. No more bookings allowed.");

			// Use Event dates if StartTime/EndTime not provided
			var startTime = request.StartTime ?? eventEntity.StartDate;
			var endTime = request.EndTime ?? eventEntity.EndDate;

			// Validate dates
			if (startTime < eventEntity.StartDate || endTime > eventEntity.EndDate)
				throw new Exception("Booking time must be within Event time range");

			var booking = new DomainLayer.Entities.Booking
			{
				Id = Guid.NewGuid(),
				UserId = currentUserId,
				RoomId = roomId,
				EventId = request.EventId,
				StartTime = startTime,
				EndTime = endTime,
				Purpose = request.Purpose ?? $"Booking for event: {eventEntity.Title}",
				Status = BookingStatus.Pending,
				Notes = request.Notes,
				CreatedAt = DateTime.UtcNow,
				LastUpdatedAt = DateTime.UtcNow
			};

            _db.Bookings.Add(booking);
			await _db.SaveChangesAsync();
            await InvalidateCaches(booking.UserId, booking.Id);
			return await GetByIdAsync(booking.Id);
		}

		public async Task<BookingDetail> UpdateStatusAsync(Guid id, UpdateBookingStatusRequest request)
		{
			var booking = await _db.Bookings
				.Include(b => b.Room)
				.Include(b => b.Event)
				.FirstOrDefaultAsync(b => b.Id == id)
				?? throw new Exception("Booking not found");

			// If approving booking, check room capacity
			if (request.Status == BookingStatus.Approved && booking.Status != BookingStatus.Approved)
			{
				// Check if room has capacity for this event
				var approvedBookingsCount = await _db.Bookings
					.CountAsync(b => b.EventId == booking.EventId && 
									 b.RoomId == booking.RoomId && 
									 b.Status == BookingStatus.Approved &&
									 b.Id != booking.Id); // Exclude current booking
				
				if (booking.Room.Capacity > 0 && approvedBookingsCount >= booking.Room.Capacity)
					throw new Exception($"Cannot approve booking. Room '{booking.Room.Name}' has reached its capacity ({booking.Room.Capacity}) for this event.");
			}

			booking.Status = request.Status;
			booking.Notes = request.Notes ?? booking.Notes;
			booking.LastUpdatedAt = DateTime.UtcNow;

            _db.Bookings.Update(booking);
			await _db.SaveChangesAsync();
            await InvalidateCaches(booking.UserId, booking.Id);
			return await GetByIdAsync(booking.Id);
		}

		public async Task DeleteAsync(Guid id)
		{
			var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id)
				?? throw new Exception("Booking not found");
            _db.Bookings.Remove(booking);
			await _db.SaveChangesAsync();
            await InvalidateCaches(booking.UserId, booking.Id);
		}

        private static string BuildListCacheKey(BookingFilterRequest? filter)
        {
            if (filter == null)
            {
                return "bookings:list:v1";
            }
            return RedisCacheKeyBuilder.Build(
                "bookings:list:v1",
                ("roomId", filter.RoomId),
                ("userId", filter.UserId),
                ("eventId", filter.EventId),
                ("status", filter.Status),
                ("from", filter.From),
                ("to", filter.To),
                ("page", filter.Page),
                ("pageSize", filter.PageSize));
        }

        private async Task InvalidateCaches(Guid userId, Guid bookingId)
        {
            await _redis.RemoveAsync("bookings:list:v1");
            await _redis.RemoveAsync(RedisCacheKeyBuilder.Build("bookings:user:v1", ("userId", userId)));
            await _redis.RemoveAsync(RedisCacheKeyBuilder.Build("bookings:detail:v1", ("id", bookingId)));
        }
	}
}


