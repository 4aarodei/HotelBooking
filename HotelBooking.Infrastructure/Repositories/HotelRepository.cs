using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities.Hotels;
using HotelBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class HotelRepository : IHotelRepository
{
    private readonly ApplicationDbContext _context;

    public HotelRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Hotel>> GetWithActiveRoomsAsync(string? city, CancellationToken ct)
    {
        var hotelsQuery = _context.Hotels.AsQueryable();

        if (!string.IsNullOrWhiteSpace(city))
        {
            var normalizedCity = city.ToLower();
            hotelsQuery = hotelsQuery.Where(h => h.City.ToLower() == normalizedCity);
        }

        return await hotelsQuery
            .Include(h => h.Rooms.Where(r => r.IsActive))
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public Task<Hotel?> GetWithRoomsByIdAsync(Guid id, CancellationToken ct)
    {
        return _context.Hotels
            .Include(h => h.Rooms.Where(r => r.IsActive))
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id, ct);
    }

    public async Task<List<Hotel>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Hotels
            .OrderBy(h => h.Name)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public Task<Hotel?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return _context.Hotels.FirstOrDefaultAsync(h => h.Id == id, ct);
    }

    public async Task AddAsync(Hotel hotel, CancellationToken ct)
    {
        _context.Hotels.Add(hotel);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Hotel hotel, CancellationToken ct)
    {
        _context.Hotels.Update(hotel);
        await _context.SaveChangesAsync(ct);
    }
}
