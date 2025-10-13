using Application.DTOs.Booking;
using DomainLayer.Entities;
using DomainLayer.Enum;
using InfrastructureLayer.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Booking
{
	public class BookingService : IBookingService
	{
		private readonly LabDbContext _db;

		public BookingService(LabDbContext db)
		{
			_db = db;
		}

		public async Task<IReadOnlyList<BookingListItem>> GetBookingsAsync(BookingFilterRequest? filter = null)
		{
			var query = _db.Bookings
				.Include(b => b.Room)
				.Include(b => b.User)
				.AsQueryable();

			if (filter != null)
			{
				if (filter.RoomId.HasValue) query = query.Where(b => b.RoomId == filter.RoomId.Value);
				if (filter.UserId.HasValue) query = query.Where(b => b.UserId == filter.UserId.Value);
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
			return items.Select(b => new BookingListItem
			{
				Id = b.Id,
				RoomId = b.RoomId,
				RoomName = b.Room.Name,
				UserId = b.UserId,
				UserName = b.User.Fullname,
				StartTime = b.StartTime,
				EndTime = b.EndTime,
				Status = b.Status
			}).ToList();
		}

		public async Task<BookingDetail> GetByIdAsync(Guid id)
		{
			var b = await _db.Bookings
				.Include(x => x.User)
				.Include(x => x.Room)
				.FirstOrDefaultAsync(x => x.Id == id)
				?? throw new Exception("Booking not found");

			return new BookingDetail
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
		}

		public async Task<BookingDetail> CreateAsync(Guid currentUserId, CreateBookingRequest request)
		{
			// Validate room and availability
			var room = await _db.Rooms.Include(r => r.Bookings).FirstOrDefaultAsync(r => r.Id == request.RoomId)
				?? throw new Exception("Room not found");

			if (room.Status != RoomStatus.Available)
				throw new Exception("Room is not available");

			var overlaps = room.Bookings.Any(b => b.Status == BookingStatus.Approved &&
				((b.StartTime <= request.StartTime && b.EndTime > request.StartTime) ||
				 (b.StartTime < request.EndTime && b.EndTime >= request.EndTime) ||
				 (b.StartTime >= request.StartTime && b.EndTime <= request.EndTime)));
			if (overlaps) throw new Exception("Room time overlaps with existing bookings");

			var booking = new DomainLayer.Entities.Booking
			{
				Id = Guid.NewGuid(),
				UserId = currentUserId,
				RoomId = request.RoomId,
				EventId = request.EventId,
				StartTime = request.StartTime,
				EndTime = request.EndTime,
				Purpose = request.Purpose,
				Status = BookingStatus.Pending,
				Notes = request.Notes,
				CreatedAt = DateTime.UtcNow,
				LastUpdatedAt = DateTime.UtcNow
			};

			_db.Bookings.Add(booking);
			await _db.SaveChangesAsync();

			return await GetByIdAsync(booking.Id);
		}

		public async Task<BookingDetail> UpdateStatusAsync(Guid id, UpdateBookingStatusRequest request)
		{
			var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id)
				?? throw new Exception("Booking not found");

			booking.Status = request.Status;
			booking.Notes = request.Notes ?? booking.Notes;
			booking.LastUpdatedAt = DateTime.UtcNow;

			_db.Bookings.Update(booking);
			await _db.SaveChangesAsync();

			return await GetByIdAsync(booking.Id);
		}

		public async Task DeleteAsync(Guid id)
		{
			var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id)
				?? throw new Exception("Booking not found");
			_db.Bookings.Remove(booking);
			await _db.SaveChangesAsync();
		}
	}
}


