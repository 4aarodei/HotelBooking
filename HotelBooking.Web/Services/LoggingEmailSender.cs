using Microsoft.AspNetCore.Identity.UI.Services;

namespace HotelBooking.Web.Services;

public sealed class LoggingEmailSender : IEmailSender
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(IWebHostEnvironment environment, ILogger<LoggingEmailSender> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (_environment.IsDevelopment())
        {
            _logger.LogInformation("Development email for {Email}: {Subject}. Body: {HtmlMessage}", email, subject, htmlMessage);
        }
        else
        {
            _logger.LogWarning("Email for {Email} with subject {Subject} was not sent because SMTP is not configured.", email, subject);
        }

        return Task.CompletedTask;
    }
}
