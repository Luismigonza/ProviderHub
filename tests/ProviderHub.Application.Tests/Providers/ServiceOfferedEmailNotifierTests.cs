using ProviderHub.Application.Abstractions.Messaging;
using ProviderHub.Application.Providers.EventHandlers;
using ProviderHub.Application.Tests.TestDoubles;
using ProviderHub.Domain.Common.ValueObjects;
using ProviderHub.Domain.Providers;
using ProviderHub.Domain.Providers.Events;
using ProviderHub.Domain.Providers.ValueObjects;
using ProviderHub.Domain.Services;

namespace ProviderHub.Application.Tests.Providers;

public class ServiceOfferedEmailNotifierTests
{
    private readonly RecordingEmailSender _emails = new();
    private readonly InMemoryServiceRepository _services = new();

    [Fact]
    public async Task Enabling_a_service_notifies_the_address_in_the_system_preferences()
    {
        var service = _services.Seed(Service.Create("Space content download", Money.Usd(120m)));
        var provider = Provider.Create(
            Nit.Create("890903938-8"),
            "Importaciones Tekus S.A.",
            WebsiteUrl.Create("https://tekus.co"),
            EmailAddress.Create("contact@tekus.co"));

        await Notifier().HandleAsync(new ServiceOfferedDomainEvent(
            provider,
            service.Id,
            [CountryCode.Create("CO"), CountryCode.Create("MX")]));

        var sent = Assert.Single(_emails.Sent);

        // The destination comes from configuration, not from the code: that is what the test
        // means by "defined in system preferences".
        Assert.Equal("operations@tekus.co", sent.To);
        Assert.Contains("Importaciones Tekus S.A.", sent.Subject, StringComparison.Ordinal);
        Assert.Contains("Space content download", sent.Body, StringComparison.Ordinal);
        Assert.Contains("890903938-8", sent.Body, StringComparison.Ordinal);
        Assert.Contains("120.00 USD", sent.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_countries_are_named_rather_than_coded()
    {
        var service = _services.Seed(Service.Create("Orbital data relay", Money.Usd(340m)));
        var provider = Provider.Create(
            Nit.Create("890903938-8"),
            "Importaciones Tekus S.A.",
            WebsiteUrl.Create("https://tekus.co"),
            EmailAddress.Create("contact@tekus.co"));

        await Notifier().HandleAsync(new ServiceOfferedDomainEvent(
            provider,
            service.Id,
            [CountryCode.Create("CO"), CountryCode.Create("MX")]));

        // "Colombia, Mexico" reads better than "CO, MX" to whoever opens the message.
        Assert.Contains("Colombia, Mexico", _emails.Sent[0].Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_service_that_disappeared_still_produces_a_message()
    {
        // The event is a fact about the past. If the catalogue entry has since been renamed away
        // or removed, that is no reason to lose the notification entirely.
        var provider = Provider.Create(
            Nit.Create("890903938-8"),
            "Importaciones Tekus S.A.",
            WebsiteUrl.Create("https://tekus.co"),
            EmailAddress.Create("contact@tekus.co"));

        await Notifier().HandleAsync(new ServiceOfferedDomainEvent(provider, 404, [CountryCode.Create("CO")]));

        Assert.Contains("service 404", Assert.Single(_emails.Sent).Body, StringComparison.Ordinal);
    }

    private ServiceOfferedEmailNotifier Notifier() =>
        new(_emails, new FixedPreferences(), _services);

    private sealed class FixedPreferences : INotificationPreferences
    {
        public string NewServiceRecipient => "operations@tekus.co";
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public List<EmailMessage> Sent { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);

            return Task.CompletedTask;
        }
    }
}
