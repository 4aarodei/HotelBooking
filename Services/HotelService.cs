using HotelBooking.Data.ApplicationDbContext;
using HotelBooking.Models.Hotels;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Services;

public interface IHotelService
{
    Task<List<Hotel>> GetAllAsync();
    Task<Hotel?> GetByIdAsync(Guid id);
    Task<List<Hotel>> GetByCityAsync(string city);
    Task<Hotel> CreateAsync(Hotel hotel);
    Task UpdateAsync(Hotel hotel);
    Task DeleteAsync(int id);
}

public class HotelService : IHotelService
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

    public async Task<Hotel> CreateAsync(Hotel hotel)
    {
        _context.Hotels.Add(hotel);
        await _context.SaveChangesAsync();
        return hotel;
    }

    public async Task UpdateAsync(Hotel hotel)
    {
        _context.Hotels.Update(hotel);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var hotel = await _context.Hotels.FindAsync(id);
        if (hotel == null) return;

        _context.Hotels.Remove(hotel);
        await _context.SaveChangesAsync();
    }
}
