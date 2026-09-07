using System.Globalization;
using System.Text;
using ProviderHub.Application.Abstractions.Events;
using ProviderHub.Application.Abstractions.Messaging;
using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Domain.Providers.Events;

namespace ProviderHub.Application.Providers.EventHandlers;

/// <summary>
/// Sends the notification the test asks for: when a provider enables a new service, an e-mail
/// goes to the address configured in the system preferences.
/// <para>
/// This lives here, and not inside <c>Provider</c>, because sending mail is not a business rule.
/// The model states that a service was enabled; deciding that somebody should hear about it is a
/// separate concern that can change, or be switched off, without touching the domain.
/// </para>
/// </summary>
public sealed class ServiceOfferedEmailNotifier(
    IEmailSender emails,
    INotificationPreferences preferences,
    IServiceRepository services) : IDomainEventHandler<ServiceOfferedDomainEvent>
{
    public async Task HandleAsync(
        ServiceOfferedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        // The event carries identifiers, as an aggregate should: it names the service, it does
        // not hold it. The readable name is looked up here, where a database is allowed.
        var service = await services
            .FindAsync(domainEvent.ServiceId, cancellationToken)
            .ConfigureAwait(false);

        var serviceName = service?.Name ?? $"service {domainEvent.ServiceId}";
        var countries = string.Join(", ", domainEvent.Countries.Select(country => country.DisplayName));

        var body = new StringBuilder()
            .AppendLine(CultureInfo.InvariantCulture, $"Provider: {domainEvent.Provider.Name}")
            .AppendLine(CultureInfo.InvariantCulture, $"NIT: {domainEvent.Provider.Nit.Value}")
            .AppendLine(CultureInfo.InvariantCulture, $"Service: {serviceName}")
            .AppendLine(CultureInfo.InvariantCulture, $"Available in: {countries}");

        if (service is not null)
        {
            body.AppendLine(CultureInfo.InvariantCulture, $"Hourly rate: {service.HourlyRate}");
        }

        var message = new EmailMessage(
            preferences.NewServiceRecipient,
            $"{domainEvent.Provider.Name} has enabled a new service",
            body.ToString());

        await emails.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }
}
