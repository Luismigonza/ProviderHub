namespace ProviderHub.Application.Abstractions.Messaging;

/// <summary>A message ready to be delivered.</summary>
/// <param name="To">Recipient address.</param>
/// <param name="Subject">Subject line.</param>
/// <param name="Body">Plain text body.</param>
public sealed record EmailMessage(string To, string Subject, string Body);

/// <summary>
/// Delivers e-mail.
/// <para>
/// The application layer knows a message goes out; it does not know whether that means SMTP, a
/// transactional e-mail API, or a file on disk during development. All three are the same idea
/// behind this interface, which is why the notification use case never had to be told.
/// </para>
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// The system preferences that decide where notifications go.
/// <para>
/// The test asks for the destination to be "defined in system preferences" rather than hard
/// coded, so it is read from configuration and reaches the use case through this port. Should it
/// ever need to be editable at runtime, the implementation moves to a settings table and nothing
/// above this line changes.
/// </para>
/// </summary>
public interface INotificationPreferences
{
    /// <summary>Who is told when a provider enables a new service.</summary>
    string NewServiceRecipient { get; }
}
