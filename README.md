# HotelBooking

![Build](https://github.com/4aarodei/HotelBooking/actions/workflows/dotnet.yml/badge.svg)

A full-featured hotel booking web application built with ASP.NET Core MVC using a clean architecture approach. Users can search hotels, browse available rooms, and make bookings, while administrators manage hotels, rooms, bookings, and users through a dedicated admin panel.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8 MVC (Razor Views) |
| ORM | Entity Framework Core 8 |
| Raw queries | Dapper |
| Database | SQL Server |
| Auth | ASP.NET Core Identity (roles: `Admin`, `User`) |
| Architecture | Clean Architecture (Core → Application → Infrastructure → Web) |

---

## Project Structure

```
HotelBooking.sln
├── HotelBooking.Core/           # Domain entities (Hotel, Room, Booking, …)
├── HotelBooking.Application/    # Business logic services & repository interfaces
├── HotelBooking.Infrastructure/ # EF Core DbContext, repository implementations, Dapper queries, migrations
└── HotelBooking.Web/            # ASP.NET Core MVC — controllers, views, view-models
    └── Areas/Admin/             # Admin panel (hotels, bookings, users, statistics)
```

---

## Features

- **Hotel search** — filter by city and date range; see availability in real time
- **Room browsing** — view room details, amenities, and photos per hotel
- **Booking** — authenticated users can create, view, and cancel their own bookings
- **User profile** — view booking history
- **Admin panel**
  - Manage hotels and rooms (CRUD)
  - Manage and review all bookings
  - User management
  - Booking statistics dashboard

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (local or remote) **or** SQL Server LocalDB (ships with Visual Studio)

### 1. Clone the repository

```bash
git clone https://github.com/4aarodei/HotelBooking.git
cd HotelBooking
```

### 2. Configure the connection string

Copy the example settings file and fill in your own connection string:

```bash
cp HotelBooking.Web/appsettings.Example.json HotelBooking.Web/appsettings.json
```

Edit `HotelBooking.Web/appsettings.json` and update `ConnectionStrings:DefaultConnection`.

### 3. Restore & build

```bash
dotnet restore
dotnet build --no-restore
```

### 4. Apply migrations

```bash
dotnet ef database update --project HotelBooking.Infrastructure --startup-project HotelBooking.Web
```

### 5. Run

```bash
dotnet run --project HotelBooking.Web
```

The app will be available at `https://localhost:5001` (or the port shown in the console).

> **Default seed accounts** (Development only, created by `IdentitySeeder`):
> check `HotelBooking.Infrastructure/Data/IdentitySeeder.cs` for credentials.

---

## Running Tests

There are currently no automated tests in the repository. (Contributions welcome!)

---

## Screenshots

> _Screenshots will be added here._

---

## License

This project is licensed under the [MIT License](LICENSE).
