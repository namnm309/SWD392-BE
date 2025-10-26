using Application.DTOs.Booking;

namespace Application.Services.Booking
{
	public interface IBookingService
	{
		Task<IReadOnlyList<BookingListItem>> GetBookingsAsync(BookingFilterRequest? filter = null);
		Task<BookingDetail> GetByIdAsync(Guid id);
		Task<IReadOnlyList<BookingListItem>> GetBookingsByUserIdAsync(Guid userId, int? page = null, int? pageSize = null);
		Task<BookingDetail> CreateAsync(Guid currentUserId, CreateBookingRequest request);
		Task<BookingDetail> UpdateStatusAsync(Guid id, UpdateBookingStatusRequest request);
		Task DeleteAsync(Guid id);
	}
}


