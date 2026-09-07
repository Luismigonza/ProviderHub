using System.ComponentModel.DataAnnotations;
using ProviderHub.Application.Abstractions.Messaging;
using Microsoft.Extensions.Options;

namespace ProviderHub.Infrastructure.Messaging;

/// <summary>How e-mail leaves this application.</summary>
public enum EmailTransport
{
    /// <summary>Writes each message to disk as an <c>.eml</c> file. The default for development.</summary>
    File = 0,

    /// <summary>Hands the message to an SMTP server.</summary>
    Smtp = 1,
}

/// <summary>
/// The system preferences the test refers to: who is notified, and how the message travels.
/// </summary>
public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    /// <summary>Address told when a provider enables a new service.</summary>
    [Required]
    [EmailAddress]
    public string NewServiceRecipient { get; set; } = string.Empty;

    /// <summary>Address the notifications are sent from.</summary>
    [Required]
    [EmailAddress]
    public string From { get; set; } = "no-reply@providerhub.local";

    public EmailTransport Transport { get; set; } = EmailTransport.File;

    /// <summary>Folder the <see cref="EmailTransport.File"/> transport writes into.</summary>
    public string OutboxDirectory { get; set; } = "outbox";

    public string SmtpHost { get; set; } = "localhost";

    [Range(1, 65535)]
    public int SmtpPort { get; set; } = 25;

    public bool SmtpUseSsl { get; set; }

    public string? SmtpUserName { get; set; }

    public string? SmtpPassword { get; set; }
}

/// <summary>
/// Reads the notification destination from configuration.
/// <para>
/// <see cref="IOptionsMonitor{T}"/> rather than <see cref="IOptions{T}"/>: a preference is
/// something an operator may want to change, and this way editing the configuration file takes
/// effect without a restart.
/// </para>
/// </summary>
internal sealed class ConfiguredNotificationPreferences(IOptionsMonitor<NotificationOptions> options)
    : INotificationPreferences
{
    public string NewServiceRecipient => options.CurrentValue.NewServiceRecipient;
}
