using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProviderHub.Application.Abstractions.Messaging;

namespace ProviderHub.Infrastructure.Messaging;

/// <summary>
/// Writes each message to disk instead of sending it.
/// <para>
/// This is the default for local work and for the tests. A reviewer running the project can open
/// the folder and read exactly what would have been sent, without an SMTP server, an account, or
/// the risk of a test suite mailing a real person.
/// </para>
/// </summary>
internal sealed partial class FileEmailSender(
    IOptionsMonitor<NotificationOptions> options,
    ILogger<FileEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var settings = options.CurrentValue;
        var directory = Path.GetFullPath(settings.OutboxDirectory);
        Directory.CreateDirectory(directory);

        var fileName = string.Create(
            CultureInfo.InvariantCulture,
            $"{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}.eml");

        var path = Path.Combine(directory, fileName);

        var contents = new StringBuilder()
            .AppendLine(CultureInfo.InvariantCulture, $"From: {settings.From}")
            .AppendLine(CultureInfo.InvariantCulture, $"To: {message.To}")
            .AppendLine(CultureInfo.InvariantCulture, $"Subject: {message.Subject}")
            .AppendLine(CultureInfo.InvariantCulture, $"Date: {DateTimeOffset.UtcNow:R}")
            .AppendLine()
            .Append(message.Body)
            .ToString();

        await File.WriteAllTextAsync(path, contents, cancellationToken).ConfigureAwait(false);

        LogWritten(logger, message.To, path);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Notification for {Recipient} written to {Path}.")]
    private static partial void LogWritten(ILogger logger, string recipient, string path);
}

/// <summary>
/// Hands the message to an SMTP server.
/// <para>
/// Built on <see cref="SmtpClient"/>, which ships with the framework and needs no dependency.
/// Its limits are worth stating rather than discovering: Microsoft steers new work towards
/// MailKit for modern authentication and better protocol support, and that is the swap to make
/// if this ever talks to a real provider. It is one class, behind an interface, precisely so
/// that swap costs nothing above this file.
/// </para>
/// </summary>
internal sealed class SmtpEmailSender(IOptionsMonitor<NotificationOptions> options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var settings = options.CurrentValue;

        using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
        {
            EnableSsl = settings.SmtpUseSsl,
            Credentials = string.IsNullOrWhiteSpace(settings.SmtpUserName)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(settings.SmtpUserName, settings.SmtpPassword),
        };

        using var mail = new MailMessage(settings.From, message.To, message.Subject, message.Body);

        await client.SendMailAsync(mail, cancellationToken).ConfigureAwait(false);
    }
}
