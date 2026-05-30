using HotelBooking.Domain.Entities.Bookings;
using HotelBooking.Domain.Entities.Hotels;
using HotelBooking.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Hotel> Hotels => Set<Hotel>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<HotelImage> HotelImages => Set<HotelImage>();
    public DbSet<RoomImage> RoomImages => Set<RoomImage>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Hotel>()
            .HasMany(h => h.Rooms)
            .WithOne(r => r.Hotel)
            .HasForeignKey(r => r.HotelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Hotel>()
            .HasMany(h => h.Images)
            .WithOne(i => i.Hotel)
            .HasForeignKey(i => i.HotelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Hotel>()
            .Property(h => h.Name)
            .HasMaxLength(150);

        builder.Entity<Hotel>()
            .Property(h => h.City)
            .HasMaxLength(120);

        builder.Entity<Hotel>()
            .HasIndex(h => h.City)
            .HasDatabaseName("IX_Hotels_City");

        builder.Entity<Hotel>()
            .HasIndex(h => h.Name)
            .HasDatabaseName("IX_Hotels_Name");

        builder.Entity<Hotel>()
            .Property(h => h.Address)
            .HasMaxLength(200);

        builder.Entity<Hotel>()
            .Property(h => h.Description)
            .HasMaxLength(1000);

        builder.Entity<Room>()
            .Property(r => r.PricePerNight)
            .HasPrecision(10, 2);

        builder.Entity<Room>()
            .Property(r => r.Name)
            .HasMaxLength(150);

        builder.Entity<Room>()
            .Property(r => r.Description)
            .HasMaxLength(1200);

        builder.Entity<Room>()
            .Property(r => r.Amenities)
            .HasMaxLength(1000);

        builder.Entity<Room>()
            .HasIndex(r => new { r.HotelId, r.IsActive })
            .HasDatabaseName("IX_Rooms_HotelId_IsActive");

        builder.Entity<Room>()
            .HasMany(r => r.Images)
            .WithOne(i => i.Room)
            .HasForeignKey(i => i.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Room>()
            .ToTable(t =>
            {
                t.HasCheckConstraint("CK_Rooms_Capacity_Positive", "[Capacity] > 0");
                t.HasCheckConstraint("CK_Rooms_PricePerNight_Positive", "[PricePerNight] > 0");
                t.HasCheckConstraint("CK_Rooms_Quantity_Positive", "[Quantity] > 0");
            });

        builder.Entity<HotelImage>()
            .Property(i => i.StorageKey)
            .HasMaxLength(600);

        builder.Entity<HotelImage>()
            .Property(i => i.Url)
            .HasMaxLength(500);

        builder.Entity<HotelImage>()
            .Property(i => i.ContentType)
            .HasMaxLength(100);

        builder.Entity<HotelImage>()
            .Property(i => i.AltText)
            .HasMaxLength(250);

        builder.Entity<HotelImage>()
            .HasIndex(i => new { i.HotelId, i.SortOrder });

        builder.Entity<HotelImage>()
            .HasIndex(i => new { i.HotelId, i.IsCover })
            .IsUnique()
            .HasFilter("[IsCover] = 1")
            .HasDatabaseName("UX_HotelImages_OneCover");

        builder.Entity<RoomImage>()
            .Property(i => i.StorageKey)
            .HasMaxLength(600);

        builder.Entity<RoomImage>()
            .Property(i => i.Url)
            .HasMaxLength(500);

        builder.Entity<RoomImage>()
            .Property(i => i.ContentType)
            .HasMaxLength(100);

        builder.Entity<RoomImage>()
            .Property(i => i.AltText)
            .HasMaxLength(250);

        builder.Entity<RoomImage>()
            .HasIndex(i => new { i.RoomId, i.SortOrder });

        builder.Entity<RoomImage>()
            .HasIndex(i => new { i.RoomId, i.IsCover })
            .IsUnique()
            .HasFilter("[IsCover] = 1")
            .HasDatabaseName("UX_RoomImages_OneCover");

        builder.Entity<Booking>()
            .HasOne(b => b.Room)
            .WithMany()
            .HasForeignKey(b => b.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Booking>()
            .HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Booking>()
            .Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Entity<Booking>()
            .Property(b => b.PricePerNightSnapshot)
            .HasPrecision(10, 2);

        builder.Entity<Booking>()
            .Property(b => b.TotalPrice)
            .HasPrecision(10, 2);

        builder.Entity<Booking>()
            .HasIndex(b => new { b.RoomId, b.CheckIn, b.CheckOut, b.Status })
            .HasDatabaseName("IX_Bookings_Room_DateRange_Status");

        builder.Entity<Booking>()
            .ToTable(t =>
            {
                t.HasCheckConstraint("CK_Bookings_DateRange", "[CheckOut] > [CheckIn]");
                t.HasCheckConstraint("CK_Bookings_Nights_Positive", "[Nights] > 0");
                t.HasCheckConstraint("CK_Bookings_PricePerNightSnapshot_Positive", "[PricePerNightSnapshot] > 0");
                t.HasCheckConstraint("CK_Bookings_TotalPrice_Positive", "[TotalPrice] > 0");
            });
    }
}
