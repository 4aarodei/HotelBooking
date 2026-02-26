# ?? HotelBooking

Веб-застосунок для пошуку та бронювання готелів, побудований на **ASP.NET Core 8 (MVC)** з використанням **Clean Architecture**.

## Архітектура

```
HotelBooking.Core (Domain)        — Сутності, константи, ролі
HotelBooking.Application          — Сервіси, інтерфейси репозиторіїв
HotelBooking.Infrastructure       — EF Core, Dapper, Repositories, Identity Seeder
HotelBooking.Web                  — Controllers, Views, ViewModels, Areas
```

### Діаграма залежностей

```
Web ? Application ? Core (Domain)
Web ? Infrastructure ? Application ? Core
```

> **Core** не залежить від жодного шару — це ядро, яке містить лише POCO-сутності та бізнес-правила.

## Технології

| Шар | Технології |

| **Web** | ASP.NET Core 8 MVC, Razor Views, Bootstrap 5.3 |
| **Application** | Сервісний шар (HotelService, BookingService, RoomService) |
| **Infrastructure** | Entity Framework Core 8, Dapper, SQL Server, ASP.NET Identity |
| **Domain** | POCO-сутності (Hotel, Room, Booking, BookingStatus, ApplicationUser) |

## Основні можливості

- ?? **Пошук готелів** — фільтрація за містом та датами, перевірка доступності номерів у реальному часі
- ??? **Бронювання** — створення бронювань з валідацією доступності, снепшот ціни на момент бронювання
- ?? **Автентифікація / Авторизація** — ASP.NET Identity з 3 ролями: `User`, `Admin`, `SuperAdmin`
- ??? **Адмін-панель** — Area `Admin` для управління готелями та номерами (CRUD)
- ?? **Статистика бронювань** — Dapper-запити для агрегації даних
- ?? **Seeding** — автоматичне створення ролей та облікового запису SuperAdmin у Development

## Предметна область

```
Hotel 1??* Room
Room 1??* Booking
Booking *??1 BookingStatus (Pending / Confirmed / Cancelled)
Booking *??1 ApplicationUser
```

## Запуск локально

### Вимоги

- .NET 8 SDK
- SQL Server (або LocalDB)

### Кроки

1. **Клонувати репозиторій:**

   ```bash
   git clone https://github.com/4aarodei/HotelBooking.git
   cd HotelBooking
   ```

2. **Налаштувати connection string через User Secrets:**

   ```bash
   cd HotelBooking.Web
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\mssqllocaldb;Database=HotelBooking;Trusted_Connection=True;MultipleActiveResultSets=true"
   ```

3. **Застосувати міграції:**

   ```bash
   dotnet ef database update --project HotelBooking.Infrastructure --startup-project HotelBooking.Web
   ```

4. **Запустити:**

   ```bash
   dotnet run --project HotelBooking.Web
   ```

> При першому запуску в `Development`-середовищі Seeder автоматично створить ролі (`SuperAdmin`, `Admin`, `User`) та обліковий запис адміністратора.
>
> **Тестовий вхід:** `admin@hotelbooking.local` / `Admin123!`

## Структура проєкту

```
??? HotelBooking.Core/             # Domain-сутності
?   ??? Entities/
?       ??? Hotels/                 # Hotel, Room
?       ??? Bookings/              # Booking, BookingStatus, BookingStatusCodes
?       ??? Identity/              # ApplicationUser, AppRoles
?
??? HotelBooking.Application/      # Бізнес-логіка
?   ??? Services/                  # HotelService, BookingService, RoomService
?   ??? Interfaces/                # IHotelRepository, IBookingRepository, IRoomRepository
?
??? HotelBooking.Infrastructure/   # Доступ до даних
?   ??? Data/                      # ApplicationDbContext, Migrations, IdentitySeeder
?   ??? Repositories/              # EF Core реалізації репозиторіїв
?   ??? Dapper/                    # DapperConnectionFactory, BookingStatisticsQuery
?
??? HotelBooking.Web/              # Презентаційний шар
    ??? Controllers/               # HomeController, HotelController, BookingController
    ??? Areas/Admin/Controllers/   # HotelsController, RoomsController, DashboardController
    ??? Views/                     # Razor Views
    ??? ViewModels/                # ViewModel-класи
```

## Ключові архітектурні рішення

1. **Clean Architecture** — шари чітко розділені; залежності спрямовані до ядра
2. **Repository Pattern** — інтерфейси в Application, реалізації в Infrastructure
3. **DI-реєстрація через extension-методи** — `AddApplication()` / `AddInfrastructure()`
4. **EF Core + Dapper** — EF для CRUD, Dapper для аналітичних запитів
5. **CancellationToken** — підтримка скасування у всіх асинхронних операціях
6. **Availability check** — перевірка доступності номерів через підрахунок активних бронювань
