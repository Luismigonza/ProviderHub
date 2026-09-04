using ProviderHub.Domain.Common;
using ProviderHub.Domain.Common.ValueObjects;

namespace ProviderHub.Domain.Providers.Events;

/// <summary>
/// Recorded when a provider enables a new service, which is the moment the test describes as
/// "a provider has enabled a new service" and the trigger of the notification e-mail.
/// <para>
/// The domain does not send the e-mail: it states the fact and moves on. Whoever cares
/// subscribes. That keeps <see cref="Provider"/> free of any knowledge about SMTP, and it means
/// adding a second reaction later, an audit entry or a webhook, changes nothing in the model.
/// </para>
/// </summary>
/// <param name="Provider">The provider that enabled the service.</param>
/// <param name="ServiceId">Identifier of the enabled service.</param>
/// <param name="Countries">Countries where the service is now offered.</param>
public sealed record ServiceOfferedDomainEvent(
    Provider Provider,
    int ServiceId,
    IReadOnlyCollection<CountryCode> Countries) : IDomainEvent
{
    /// <inheritdoc />
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
