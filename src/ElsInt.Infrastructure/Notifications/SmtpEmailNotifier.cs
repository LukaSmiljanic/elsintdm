using System.Net;
using System.Net.Mail;
using ElsInt.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ElsInt.Infrastructure.Notifications;

/// <summary>
/// SMTP sender. Config shape matches SalonPro's working Gmail app-password setup
/// (Email:SmtpHost/Port/User/Pass) so the same mailbox can notify ElsInt bookings immediately.
/// </summary>
public class SmtpEmailNotifier : IEmailNotifier
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailNotifier> _logger;

    public SmtpEmailNotifier(IConfiguration config, ILogger<SmtpEmailNotifier> logger)
    {
        _config = config;
        _logger = logger;
    }

    public Task NotifyAsync(string subject, string body, CancellationToken cancellationToken = default)
    {
        var to = _config["Email:NotifyTo"];
        return SendToManyAsync(to, subject, body, cancellationToken);
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        => SendToManyAsync(to, subject, body, cancellationToken);

    private async Task SendToManyAsync(string? recipients, string subject, string body, CancellationToken cancellationToken)
    {
        var enabled = _config.GetValue("Email:Enabled", false);
        var host = _config["Email:SmtpHost"];
        if (!enabled || string.IsNullOrWhiteSpace(host))
        {
            _logger.LogInformation("Email skipped (disabled or host missing). Subject={Subject}", subject);
            return;
        }

        var addresses = (recipients ?? string.Empty)
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(a => a.Contains('@'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (addresses.Count == 0)
        {
            _logger.LogInformation("Email skipped (no recipients). Subject={Subject}", subject);
            return;
        }

        var port = _config.GetValue("Email:SmtpPort", 587);
        var user = _config["Email:SmtpUser"];
        var pass = _config["Email:SmtpPass"];
        var fromAddress = _config["Email:From"] ?? user ?? "noreply@elsintdm.rs";
        var fromName = _config["Email:FromName"] ?? "ElsInt";
        var useSsl = _config.GetValue("Email:UseSsl", true);

        try
        {
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = useSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 20000
            };
            if (!string.IsNullOrWhiteSpace(user))
                client.Credentials = new NetworkCredential(user, pass);

            foreach (var to in addresses)
            {
                using var message = new MailMessage
                {
                    From = new MailAddress(fromAddress, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false,
                    BodyEncoding = System.Text.Encoding.UTF8,
                    SubjectEncoding = System.Text.Encoding.UTF8
                };
                message.To.Add(to);
                await client.SendMailAsync(message, cancellationToken);
                _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
            }
        }
        catch (Exception ex)
        {
            // Never fail the booking flow because mail is down.
            _logger.LogError(ex, "Failed to send email. Subject={Subject}", subject);
        }
    }
}
