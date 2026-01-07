using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities.Bookings;

namespace HotelBooking.Application.Services;

public class BookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingStatusRepository _bookingStatusRepository;
    private readonly IRoomRepository _roomRepository;

    public BookingService(
        IBookingRepository bookingRepository,
        IBookingStatusRepository bookingStatusRepository,
        IRoomRepository roomRepository)
    {
        _bookingRepository = bookingRepository;
        _bookingStatusRepository = bookingStatusRepository;
        _roomRepository = roomRepository;
    }

    public async Task<Booking> CreateAsync(string userId, Guid roomId, DateTime checkIn, DateTime checkOut, CancellationToken ct = default)
    {
        checkIn = checkIn.Date;
        checkOut = checkOut.Date;

        if (checkOut <= checkIn)
        {
            throw new InvalidOperationException("Check-out має бути пізніше Check-in");
        }

        var room = await _roomRepository.GetByIdAsync(roomId, ct)
                   ?? throw new InvalidOperationException("Кімната не знайдена");

        if (!room.IsActive)
        {
            throw new InvalidOperationException("Кімната недоступна для бронювання");
        }

        var bookingsByRoom = await _bookingRepository.GetActiveBookingsCountByRoomAsync(
            new[] { roomId },
            checkIn,
            checkOut,
            BookingStatusCodes.Cancelled,
            ct);

        if (bookingsByRoom.GetValueOrDefault(roomId, 0) >= room.Quantity)
        {
            throw new InvalidOperationException("Кімната вже заброньована на ці дати");
        }

        var pendingStatus = await _bookingStatusRepository.GetByCodeAsync(BookingStatusCodes.Pending, ct)
                            ?? throw new InvalidOperationException("Статус очікування не знайдено");

        var booking = new Booking
        {
            UserId = userId,
            RoomId = roomId,
            StatusId = pendingStatus.Id,
            CheckIn = checkIn,
            CheckOut = checkOut,
            PricePerNightSnapshot = room.PricePerNight,
            Nights = (checkOut - checkIn).Days,
            TotalPrice = (checkOut - checkIn).Days * room.PricePerNight,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _bookingRepository.AddAsync(booking, ct);

        return booking;
    }
    public Task<List<Booking>> GetByUserAsync(string userId, CancellationToken ct = default) =>
        _bookingRepository.GetByUserAsync(userId, ct);
}