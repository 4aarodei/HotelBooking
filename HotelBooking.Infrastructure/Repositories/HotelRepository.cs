using HotelBooking.Application.Persistence;
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
            var normalizedCity = city.Trim();
            hotelsQuery = hotelsQuery.Where(h => h.City == normalizedCity);
        }

        return await hotelsQuery
            .Include(h => h.Images)
            .Include(h => h.Rooms.Where(r => r.IsActive))
            .ThenInclude(r => r.Images)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public Task<Hotel?> GetWithRoomsByIdAsync(Guid id, CancellationToken ct)
    {
        return _context.Hotels
            .Include(h => h.Images)
            .Include(h => h.Rooms.Where(r => r.IsActive))
            .ThenInclude(r => r.Images)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id, ct);
    }

    public Task<List<string>> GetDistinctCitiesAsync(CancellationToken ct)
    {
        return _context.Hotels
            .Where(h => h.City != string.Empty)
            .Select(h => h.City.Trim())
            .Distinct()
            .OrderBy(city => city)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public Task<List<Hotel>> GetFeaturedAsync(int count, CancellationToken ct)
    {
        return _context.Hotels
            .Include(h => h.Images)
            .Include(h => h.Rooms.Where(r => r.IsActive))
            .OrderBy(h => h.Name)
            .Take(count)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<Hotel>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Hotels
            .Include(h => h.Images)
            .OrderBy(h => h.Name)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public Task<Hotel?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return _context.Hotels.FirstOrDefaultAsync(h => h.Id == id, ct);
    }

    public Task<Hotel?> GetByIdWithImagesAsync(Guid id, CancellationToken ct)
    {
        return _context.Hotels
            .Include(h => h.Images)
            .Include(h => h.Rooms)
            .ThenInclude(r => r.Images)
            .AsSplitQuery()
            .FirstOrDefaultAsync(h => h.Id == id, ct);
    }

    public async Task AddAsync(Hotel hotel, CancellationToken ct)
    {
        _context.Hotels.Add(hotel);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Hotel hotel, CancellationToken ct)
    {
        var entry = _context.Entry(hotel);
        if (entry.State == EntityState.Detached)
        {
            _context.Hotels.Attach(hotel);
        }

        await _context.SaveChangesAsync(ct);
    }
}
