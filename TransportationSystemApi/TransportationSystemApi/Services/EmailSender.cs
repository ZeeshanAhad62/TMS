using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace TransportationSystemApi.Services;

public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromEmail { get; set; } = "alerts@example.com";
    public string FromName { get; set; } = "Fleet Master Compliance";
}

public interface IEmailSender
{
    Task<bool> TrySendAsync(IEnumerable<string> recipients, string subject, string body, CancellationToken ct = default);
}

// Thin wrapper over System.Net.Mail so the compliance job has one place to
// send from. Host left blank (the dev default) means "not configured yet" --
// TrySendAsync logs and returns false instead of throwing, so the daily scan
// keeps running without a mail server present.
public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> TrySendAsync(IEnumerable<string> recipients, string subject, string body, CancellationToken ct = default)
    {
        var recipientList = recipients.Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
        if (recipientList.Count == 0) return false;

        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogWarning("Smtp:Host is not configured; skipping compliance alert email \"{Subject}\" to {Recipients}.",
                subject, string.Join(", ", recipientList));
            return false;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = subject,
            Body = body
        };
        foreach (var recipient in recipientList)
            message.To.Add(recipient);

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = string.IsNullOrWhiteSpace(_options.Username)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_options.Username, _options.Password)
        };

        try
        {
            await client.SendMailAsync(message, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send compliance alert email \"{Subject}\" to {Recipients}.",
                subject, string.Join(", ", recipientList));
            return false;
        }
    }
}
