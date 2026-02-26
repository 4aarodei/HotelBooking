🏨 HotelBooking

Веб-застосунок для пошуку та бронювання готелів, побудований на ASP.NET Core 8 (MVC) з використанням принципів Clean Architecture.

🧱 Архітектура
Шари системи

HotelBooking.Core (Domain)
    └─ Сутності, бізнес-правила, ролі
HotelBooking.Application
    └─ Сервіси, інтерфейси репозиторіїв
HotelBooking.Infrastructure
    └─ EF Core, Dapper, Identity, реалізації репозиторіїв
HotelBooking.Web
    └─ Controllers, Views, ViewModels, Areas

✅ Core не залежить від жодного шару
✅ Залежності спрямовані всередину (Dependency Rule)
✅ Infrastructure реалізує контракти з Application

🛠 Технології
Шар	Технології
Web	ASP.NET Core 8 MVC, Razor Views, Bootstrap 5.3
Application	Сервісний шар (HotelService, BookingService, RoomService)
Infrastructure	Entity Framework Core 8, Dapper, SQL Server, ASP.NET Identity
Domain	POCO-сутності (Hotel, Room, Booking, BookingStatus, ApplicationUser)
✨ Основні можливості
🔍 Пошук готелів

Фільтрація за містом та датами

Перевірка доступності номерів у реальному часі

🏨 Бронювання

Валідація доступності номерів

Снепшот ціни на момент бронювання

Статуси: Pending, Confirmed, Cancelled

🔐 Авторизація

ASP.NET Identity

Ролі: User, Admin, SuperAdmin

🛠 Адмін-панель

Area Admin

CRUD для готелів та номерів

Dashboard зі статистикою

📊 Статистика

Dapper для агрегаційних запитів

Оптимізація читання через lightweight ORM

🌱 Seeding

Автоматичне створення ролей

Створення SuperAdmin в середовищі Development

🧩 Предметна область
Hotel 1 ── * Room
Room 1 ── * Booking
Booking * ── 1 BookingStatus
Booking * ── 1 ApplicationUser
🚀 Запуск локально
Вимоги

.NET 8 SDK
SQL Server або LocalDB

1️⃣ Клонування репозиторію
git clone https://github.com/4aarodei/HotelBooking.git
cd HotelBooking
2️⃣ Налаштування connection string (User Secrets)
cd HotelBooking.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\mssqllocaldb;Database=HotelBooking;Trusted_Connection=True;MultipleActiveResultSets=true"
3️⃣ Застосування міграцій
dotnet ef database update --project HotelBooking.Infrastructure --startup-project HotelBooking.Web
4️⃣ Запуск застосунку
dotnet run --project HotelBooking.Web
🔑 Тестовий акаунт (Development)
Email:    admin@hotelbooking.local
Password: Admin123!
📁 Структура проєкту
HotelBooking/
│
├── HotelBooking.Core/
│   ├── Entities/
│   │   ├── Hotels/
│   │   ├── Bookings/
│   │   └── Identity/
│
├── HotelBooking.Application/
│   ├── Services/
│   └── Interfaces/
│
├── HotelBooking.Infrastructure/
│   ├── Data/
│   ├── Repositories/
│   └── Dapper/
│
└── HotelBooking.Web/
    ├── Controllers/
    ├── Areas/Admin/Controllers/
    ├── Views/
    └── ViewModels/
🏗 Ключові архітектурні рішення
✔ Clean Architecture

Чітке розділення відповідальностей та контроль напрямку залежностей.

✔ Repository Pattern

Інтерфейси в Application, реалізації в Infrastructure.

✔ DI через Extension Methods
builder.Services
    .AddApplication()
    .AddInfrastructure(configuration);
✔ EF Core + Dapper

EF Core — транзакційний CRUD

Dapper — оптимізовані аналітичні запити

✔ CancellationToken

Підтримка скасування у всіх асинхронних операціях.

✔ Availability Check

Перевірка доступності номерів через підрахунок активних бронювань.
