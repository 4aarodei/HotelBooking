using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MediaStorageMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoomImages_RoomId_IsCover",
                table: "RoomImages");

            migrationBuilder.DropIndex(
                name: "IX_HotelImages_HotelId_IsCover",
                table: "HotelImages");

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "RoomImages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "RoomImages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "SizeBytes",
                table: "RoomImages",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "RoomImages",
                type: "nvarchar(600)",
                maxLength: 600,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "RoomImages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "HotelImages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "HotelImages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "SizeBytes",
                table: "HotelImages",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "HotelImages",
                type: "nvarchar(600)",
                maxLength: 600,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "HotelImages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE RoomImages
                SET StorageKey = CASE
                        WHEN Url LIKE '/uploads/%' THEN SUBSTRING(Url, 10, 600)
                        ELSE Url
                    END,
                    ContentType = CASE
                        WHEN LOWER(Url) LIKE '%.png' THEN 'image/png'
                        WHEN LOWER(Url) LIKE '%.jpg' OR LOWER(Url) LIKE '%.jpeg' THEN 'image/jpeg'
                        WHEN LOWER(Url) LIKE '%.webp' THEN 'image/webp'
                        ELSE 'application/octet-stream'
                    END;

                UPDATE HotelImages
                SET StorageKey = CASE
                        WHEN Url LIKE '/uploads/%' THEN SUBSTRING(Url, 10, 600)
                        ELSE Url
                    END,
                    ContentType = CASE
                        WHEN LOWER(Url) LIKE '%.png' THEN 'image/png'
                        WHEN LOWER(Url) LIKE '%.jpg' OR LOWER(Url) LIKE '%.jpeg' THEN 'image/jpeg'
                        WHEN LOWER(Url) LIKE '%.webp' THEN 'image/webp'
                        ELSE 'application/octet-stream'
                    END;

                INSERT INTO RoomImages
                    (Id, RoomId, StorageKey, Url, ContentType, SizeBytes, Width, Height, AltText, IsCover, SortOrder, CreatedAtUtc)
                SELECT
                    NEWID(),
                    r.Id,
                    CASE
                        WHEN r.ImageUrl LIKE '/uploads/%' THEN SUBSTRING(r.ImageUrl, 10, 600)
                        ELSE r.ImageUrl
                    END,
                    r.ImageUrl,
                    CASE
                        WHEN LOWER(r.ImageUrl) LIKE '%.png' THEN 'image/png'
                        WHEN LOWER(r.ImageUrl) LIKE '%.jpg' OR LOWER(r.ImageUrl) LIKE '%.jpeg' THEN 'image/jpeg'
                        WHEN LOWER(r.ImageUrl) LIKE '%.webp' THEN 'image/webp'
                        ELSE 'application/octet-stream'
                    END,
                    0,
                    0,
                    0,
                    r.Name,
                    1,
                    0,
                    SYSDATETIMEOFFSET()
                FROM Rooms r
                WHERE r.ImageUrl IS NOT NULL
                    AND LTRIM(RTRIM(r.ImageUrl)) <> ''
                    AND NOT EXISTS (
                        SELECT 1
                        FROM RoomImages ri
                        WHERE ri.RoomId = r.Id
                    );

                WITH RankedHotelImages AS (
                    SELECT Id,
                           ROW_NUMBER() OVER (PARTITION BY HotelId ORDER BY IsCover DESC, SortOrder ASC, CreatedAtUtc ASC, Id ASC) AS rn
                    FROM HotelImages
                )
                UPDATE hi
                SET IsCover = CASE WHEN ranked.rn = 1 THEN 1 ELSE 0 END
                FROM HotelImages hi
                INNER JOIN RankedHotelImages ranked ON ranked.Id = hi.Id;

                WITH RankedRoomImages AS (
                    SELECT Id,
                           ROW_NUMBER() OVER (PARTITION BY RoomId ORDER BY IsCover DESC, SortOrder ASC, CreatedAtUtc ASC, Id ASC) AS rn
                    FROM RoomImages
                )
                UPDATE ri
                SET IsCover = CASE WHEN ranked.rn = 1 THEN 1 ELSE 0 END
                FROM RoomImages ri
                INNER JOIN RankedRoomImages ranked ON ranked.Id = ri.Id;
                """);

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Rooms");

            migrationBuilder.CreateIndex(
                name: "UX_RoomImages_OneCover",
                table: "RoomImages",
                columns: new[] { "RoomId", "IsCover" },
                unique: true,
                filter: "[IsCover] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Hotels_City",
                table: "Hotels",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_Hotels_Name",
                table: "Hotels",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "UX_HotelImages_OneCover",
                table: "HotelImages",
                columns: new[] { "HotelId", "IsCover" },
                unique: true,
                filter: "[IsCover] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_RoomImages_OneCover",
                table: "RoomImages");

            migrationBuilder.DropIndex(
                name: "IX_Hotels_City",
                table: "Hotels");

            migrationBuilder.DropIndex(
                name: "IX_Hotels_Name",
                table: "Hotels");

            migrationBuilder.DropIndex(
                name: "UX_HotelImages_OneCover",
                table: "HotelImages");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "RoomImages");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "RoomImages");

            migrationBuilder.DropColumn(
                name: "SizeBytes",
                table: "RoomImages");

            migrationBuilder.DropColumn(
                name: "StorageKey",
                table: "RoomImages");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "RoomImages");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "HotelImages");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "HotelImages");

            migrationBuilder.DropColumn(
                name: "SizeBytes",
                table: "HotelImages");

            migrationBuilder.DropColumn(
                name: "StorageKey",
                table: "HotelImages");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "HotelImages");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Rooms",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomImages_RoomId_IsCover",
                table: "RoomImages",
                columns: new[] { "RoomId", "IsCover" });

            migrationBuilder.CreateIndex(
                name: "IX_HotelImages_HotelId_IsCover",
                table: "HotelImages",
                columns: new[] { "HotelId", "IsCover" });
        }
    }
}
