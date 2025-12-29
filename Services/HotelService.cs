using HotelBooking.Constants;
using HotelBooking.Data.ApplicationDbContext;
using HotelBooking.Models.Hotels;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Services;

public class HotelService
{
    private readonly ApplicationDbContext _context;

    public HotelService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Hotel>> GetByCityAsync(string city)
    {
        return await _context.Hotels
            .Where(h => h.City.ToLower() == city.ToLower())
            .Include(h => h.Rooms)
            .ToListAsync();
    }

    public async Task<List<Hotel>> GetAllAsync()
    {
        return await _context.Hotels
            .Include(h => h.Rooms)
            .ToListAsync();
    }

    public async Task<Hotel?> GetByIdAsync(Guid id)
    {
        return await _context.Hotels
            .Include(h => h.Rooms)
            .FirstOrDefaultAsync(h => h.Id == id);
    }
}
