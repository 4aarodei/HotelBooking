namespace HotelBooking.Web.Startup;

// Small DTO with validated startup values.
internal sealed record RuntimeSettings(
    bool RequireConfirmedAccount,
    bool HasSmtpEmail,
    string ConnectionString,
    string ImageStorageProvider);
