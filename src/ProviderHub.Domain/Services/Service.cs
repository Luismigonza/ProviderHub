using ProviderHub.Domain.Common;
using ProviderHub.Domain.Common.ValueObjects;

namespace ProviderHub.Domain.Services;

/// <summary>
/// A service that providers can offer, such as "Space content download", together with the
/// price of one hour of it.
/// <para>
/// A service exists on its own, independently of any provider: several providers may offer the
/// same catalogue entry. That is why it is its own aggregate root rather than a child of
/// <see cref="Providers.Provider"/>.
/// </para>
/// </summary>
public sealed class Service : AggregateRoot
{
    public const int NameMaxLength = 200;

    /// <summary>Required by the persistence layer. See the note on <c>Provider</c>.</summary>
    private Service()
    {
        Name = null!;
        HourlyRate = null!;
    }

    private Service(string name, Money hourlyRate)
    {
        Name = name;
        HourlyRate = hourlyRate;
    }

    public string Name { get; private set; }

    /// <summary>Price of one hour of this service.</summary>
    public Money HourlyRate { get; private set; }

    /// <exception cref="DomainException">The name is missing or too long.</exception>
    public static Service Create(string? name, Money hourlyRate)
    {
        ArgumentNullException.ThrowIfNull(hourlyRate);

        return new Service(DomainGuard.RequiredText(name, NameMaxLength, "Service name"), hourlyRate);
    }

    /// <exception cref="DomainException">The name is missing or too long.</exception>
    public void Rename(string? name) =>
        Name = DomainGuard.RequiredText(name, NameMaxLength, "Service name");

    public void ChangeHourlyRate(Money hourlyRate)
    {
        ArgumentNullException.ThrowIfNull(hourlyRate);

        HourlyRate = hourlyRate;
    }
}
