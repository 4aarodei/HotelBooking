<div align="center">

<img src="https://raw.githubusercontent.com/Tarikul-Islam-Anik/Animated-Fluent-Emojis/master/Emojis/Travel%20and%20places/Hotel.png" alt="Hotel" width="80" />

#  HotelBooking

**Сучасний веб-застосунок для пошуку та бронювання готелів**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-MVC-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core)
[![Entity Framework](https://img.shields.io/badge/EF_Core-8.0-512BD4?style=for-the-badge&logo=nuget&logoColor=white)](https://learn.microsoft.com/ef/core/)
[![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![Bootstrap](https://img.shields.io/badge/Bootstrap-5.3-7952B3?style=for-the-badge&logo=bootstrap&logoColor=white)](https://getbootstrap.com/)

</div>

---

## 📐 Архітектура

Проєкт побудований за принципами **Clean Architecture** — чіткий розподіл відповідальностей між шарами, залежності спрямовані до ядра домену.

```
┌─────────────────────────────────────────────────────────┐
│                    HotelBooking.Web                     │
│         Controllers · Views · ViewModels · Areas        │
├─────────────────────────────────────────────────────────┤
│                HotelBooking.Infrastructure              │
│      EF Core · Dapper · Repositories · Identity Seeder  │
├─────────────────────────────────────────────────────────┤
│                 HotelBooking.Application                │
│         Services · Repository Interfaces · DTOs         │
├─────────────────────────────────────────────────────────┤
│                    HotelBooking.Core                    │
│          POCO Entities · Business Rules · Roles         │
└─────────────────────────────────────────────────────────┘
```

> **Core** не залежить від жодного іншого шару — це чисте доменне ядро без зовнішніх залежностей.

---

## 🗂️ Структура проєкту

```
HotelBooking/
├── HotelBooking.Core/               # 🔵 Domain — сутності та бізнес-правила
│   └── Entities/
│       ├── Hotels/                  # Hotel, Room
│       ├── Bookings/                # Booking, BookingStatus, BookingStatusCodes
│       └── Identity/                # ApplicationUser, AppRoles
│
├── HotelBooking.Application/        # 🟢 Application — бізнес-логіка
│   ├── Services/                    # HotelService, BookingService, RoomService
│   └── Interfaces/                  # IHotelRepository, IBookingRepository, IRoomRepository
│
├── HotelBooking.Infrastructure/     # 🟡 Infrastructure — доступ до даних
│   ├── Data/                        # ApplicationDbContext, Migrations, IdentitySeeder
│   ├── Repositories/                # EF Core реалізації репозиторіїв
│   └── Dapper/                      # DapperConnectionFactory, BookingStatisticsQuery
│
└── HotelBooking.Web/                # 🔴 Presentation — UI та API
    ├── Controllers/                  # HomeController, HotelController, BookingController
    ├── Areas/Admin/Controllers/      # HotelsController, RoomsController, DashboardController
    ├── Views/                        # Razor Views
    └── ViewModels/                   # ViewModel-класи
```

---

## ✨ Функціональність

| Можливість | Опис |
|---|---|
| 🔍 **Пошук готелів** | Фільтрація за містом та датами, перевірка доступності в реальному часі |
| 📅 **Бронювання** | Створення бронювань з валідацією доступності та снепшотом ціни |
| 🔐 **Автентифікація** | ASP.NET Identity з роллю `User`, `Admin`, `SuperAdmin` |
| 🛠️ **Адмін-панель** | Area `Admin` — повний CRUD для готелів та номерів |
| 📊 **Статистика** | Dapper-запити для агрегації та аналітики бронювань |
| 🌱 **Auto Seeding** | Автоматичне створення ролей та SuperAdmin у Development |

---

## 🗺️ Предметна область

```
Hotel ──────┐
            │ 1 : many
            ▼
           Room ────┐
                    │ 1 : many
                    ▼
                 Booking ──────► BookingStatus  (Pending / Confirmed / Cancelled)
                    │
                    └──────────► ApplicationUser
```

---

## 🛠️ Технологічний стек

| Шар | Технології |
|---|---|
| **Web** | ASP.NET Core 8 MVC, Razor Views, Bootstrap 5.3 |
| **Application** | Service Layer (HotelService, BookingService, RoomService) |
| **Infrastructure** | Entity Framework Core 8, Dapper, SQL Server, ASP.NET Identity |
| **Domain** | POCO Entities (Hotel, Room, Booking, BookingStatus, ApplicationUser) |

---

## ⚙️ Ключові архітектурні рішення

**Clean Architecture** — шари чітко розділені; залежності спрямовані виключно до ядра домену.

**Repository Pattern** — інтерфейси визначені в `Application`, реалізації — в `Infrastructure`. Це дозволяє легко замінити джерело даних без змін у бізнес-логіці.

**EF Core + Dapper** — EF Core для стандартних CRUD-операцій, Dapper для складних аналітичних запитів, де важлива продуктивність.

**DI через extension-методи** — `AddApplication()` / `AddInfrastructure()` забезпечують чисту реєстрацію залежностей без "забруднення" `Program.cs`.

**CancellationToken** — підтримка скасування у всіх асинхронних операціях для коректної обробки запитів.

---

## 🚀 Запуск локально

### Вимоги

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server або [LocalDB](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb)

### Кроки

**1. Клонувати репозиторій**

```bash
git clone https://github.com/4aarodei/HotelBooking.git
cd HotelBooking
```

**2. Налаштувати рядок підключення через User Secrets**

```bash
cd HotelBooking.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=(localdb)\mssqllocaldb;Database=HotelBooking;Trusted_Connection=True;MultipleActiveResultSets=true"
```

**3. Застосувати міграції бази даних**

```bash
dotnet ef database update \
  --project HotelBooking.Infrastructure \
  --startup-project HotelBooking.Web
```

**4. Запустити застосунок**

```bash
dotnet run --project HotelBooking.Web
```

> 🌱 При першому запуску в `Development`-середовищі **Seeder** автоматично створить ролі (`SuperAdmin`, `Admin`, `User`) та обліковий запис адміністратора.

### 🔑 Тестовий доступ

```
Email:    admin@hotelbooking.local
Password: Admin123!
```

---

<div align="center">

Made with ❤️ using **ASP.NET Core 8** & **Clean Architecture**

</div>
