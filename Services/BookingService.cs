using HotelBooking.Data.ApplicationDbContext;
using HotelBooking.Models.Bookings;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Services;

public interface IBookingService
{
    Task<Booking> CreateAsync(
        int roomId,
        string userId,
        DateTime checkIn,
        DateTime checkOut);

    Task<List<Booking>> GetUserBookingsAsync(string userId);
    Task<List<Booking>> GetAllAsync();
    Task ChangeStatusAsync(int bookingId, string statusCode);
}

public class BookingService : IBookingService
{
    private readonly ApplicationDbContext _context;

    public BookingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Booking> CreateAsync(Guid roomId, string userId, DateTime checkIn, DateTime checkOut)
    {
        if (checkIn >= checkOut)
            throw new Exception("Invalid dates");

        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null)
            throw new Exception("Room not found");

        var isBusy = await _context.Bookings.AnyAsync(b =>
            b.RoomId == roomId &&
            checkIn < b.CheckOut &&
            checkOut > b.CheckIn);

        if (isBusy)
            throw new Exception("Room is already booked");

        var status = await _context.BookingStatuses
            .FirstAsync(s => s.Code == "NEW");

        var days = (checkOut - checkIn).Days;

        var booking = new Booking
        {
            RoomId = roomId,
            UserId = userId,
            StatusId = status.Id,
            CheckIn = checkIn,
            CheckOut = checkOut,
            PricePerNightSnapshot = room.PricePerNight,
            TotalPrice = room.PricePerNight * days
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        return booking;
    }

    public async Task<List<Booking>> GetUserBookingsAsync(string userId)
    {
        return await _context.Bookings
            .Include(b => b.Room)
                .ThenInclude(r => r.Hotel)
            .Include(b => b.Status)
            .Where(b => b.UserId == userId)
            .ToListAsync();
    }

    public async Task<List<Booking>> GetAllAsync()
    {
        return await _context.Bookings
            .Include(b => b.Room)
            .Include(b => b.User)
            .Include(b => b.Status)
            .ToListAsync();
    }

    public async Task ChangeStatusAsync(int bookingId, string statusCode)
    {
        var booking = await _context.Bookings.FindAsync(bookingId);
        if (booking == null)
            throw new Exception("Booking not found");

        var status = await _context.BookingStatuses
            .FirstOrDefaultAsync(s => s.Code == statusCode);

        if (status == null)
            throw new Exception("Invalid status");

        booking.StatusId = status.Id;
        await _context.SaveChangesAsync();
    }
}
