# HotelBooking

Демо-проєкт системи бронювання готелів на ASP.NET Core 8 (Clean Architecture + Identity + EF Core).

## Що підготовлено для співбесіди

- Автоматичний сід ролей, демо-користувачів, статусів бронювання та тестових готелів у `Development`.
- Швидкий старт для локальної демонстрації без ручного наповнення БД.
- Прибрано продакшн-секрети з репозиторію.

## Демо-акаунти

> Створюються автоматично при старті застосунку в `Development`.

- **SuperAdmin**: `superadmin@hotelbooking.demo` / `Demo123!`
- **Admin**: `admin@hotelbooking.demo` / `Demo123!`
- **User**: `user@hotelbooking.demo` / `Demo123!`

## Як запустити локально

1. Переконайтесь, що встановлено .NET 8 SDK.
2. Оновіть connection string в `HotelBooking.Web/appsettings.Development.json` під вашу локальну БД SQL Server (за замовчуванням LocalDB).
3. Запустіть веб-проєкт:

   ```bash
   dotnet run --project HotelBooking.Web
   ```

4. Відкрийте застосунок у браузері та увійдіть одним із демо-акаунтів.

## Демо-сценарій для співбесіди (5–10 хв)

1. **Home / каталог готелів** — показати список та фільтрацію по місту/датам.
2. **Details готелю** — показати доступні номери та ціну.
3. **Бронювання** (роль User) — створити бронювання на майбутні дати.
4. **Профіль користувача** — переглянути власні бронювання.
5. **Admin area** (роль Admin/SuperAdmin) — показати керування готелями та статистику.
6. **Users area** (роль SuperAdmin) — показати керування ролями.

## Архітектура

- `HotelBooking.Core` — доменні сутності.
- `HotelBooking.Application` — use-cases/сервіси.
- `HotelBooking.Infrastructure` — EF Core, Dapper, репозиторії.
- `HotelBooking.Web` — MVC UI + Identity.

## Нотатка

У production середовищі вмикайте підтвердження акаунтів та використовуйте секрети через `User Secrets` / CI variables / Secret Manager, а не через `appsettings.json`.
