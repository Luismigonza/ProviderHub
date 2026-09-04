using ProviderHub.Domain.Common;
using ProviderHub.Domain.Common.ValueObjects;
using ProviderHub.Domain.Providers.Events;
using ProviderHub.Domain.Providers.ValueObjects;

namespace ProviderHub.Domain.Providers;

/// <summary>
/// A company that offers services through the platform, for example "Importaciones Tekus S.A.".
/// <para>
/// Root of the provider aggregate: its offerings are reachable only through it, so every rule
/// about what a provider may or may not offer is enforced in one place.
/// </para>
/// </summary>
public sealed class Provider : AggregateRoot
{
    public const int NameMaxLength = 200;

    private readonly List<ServiceOffering> _offerings = [];

    private Provider(Nit nit, string name, WebsiteUrl website, EmailAddress email)
    {
        Nit = nit;
        Name = name;
        Website = website;
        Email = email;
    }

    /// <summary>Colombian tax identifier. Unique across providers.</summary>
    public Nit Nit { get; private set; }

    public string Name { get; private set; }

    public WebsiteUrl Website { get; private set; }

    public EmailAddress Email { get; private set; }

    /// <summary>Services this provider offers, and where.</summary>
    public IReadOnlyCollection<ServiceOffering> Offerings => _offerings.AsReadOnly();

    /// <exception cref="DomainException">The name is missing or too long.</exception>
    public static Provider Create(Nit nit, string? name, WebsiteUrl website, EmailAddress email)
    {
        ArgumentNullException.ThrowIfNull(nit);
        ArgumentNullException.ThrowIfNull(website);
        ArgumentNullException.ThrowIfNull(email);

        return new Provider(nit, DomainGuard.RequiredText(name, NameMaxLength, "Provider name"), website, email);
    }

    /// <exception cref="DomainException">The name is missing or too long.</exception>
    public void Rename(string? name) =>
        Name = DomainGuard.RequiredText(name, NameMaxLength, "Provider name");

    /// <summary>
    /// Corrects the tax identifier. Uniqueness across providers is not something this instance
    /// can know about, so it is checked by the use case before calling this.
    /// </summary>
    public void ChangeNit(Nit nit)
    {
        ArgumentNullException.ThrowIfNull(nit);

        Nit = nit;
    }

    public void ChangeContactDetails(WebsiteUrl website, EmailAddress email)
    {
        ArgumentNullException.ThrowIfNull(website);
        ArgumentNullException.ThrowIfNull(email);

        Website = website;
        Email = email;
    }

    /// <summary>Indicates whether this provider already offers the given service.</summary>
    public bool Offers(int serviceId) => _offerings.Exists(offering => offering.ServiceId == serviceId);

    /// <summary>
    /// Enables a service for this provider in the given countries and records a
    /// <see cref="ServiceOfferedDomainEvent"/>.
    /// </summary>
    /// <exception cref="DomainException">
    /// The service is already offered, or no valid country was supplied.
    /// </exception>
    public ServiceOffering OfferService(int serviceId, IEnumerable<CountryCode> countries)
    {
        if (Offers(serviceId))
        {
            throw new DomainException($"This provider already offers service {serviceId}.");
        }

        var offering = new ServiceOffering(serviceId, countries);
        _offerings.Add(offering);

        Raise(new ServiceOfferedDomainEvent(this, serviceId, offering.Countries));

        return offering;
    }

    /// <summary>Replaces the countries where an already offered service is available.</summary>
    /// <exception cref="DomainException">The service is not offered by this provider.</exception>
    public void ChangeOfferedCountries(int serviceId, IEnumerable<CountryCode> countries) =>
        RequireOffering(serviceId).ChangeCountries(countries);

    /// <summary>Stops offering a service.</summary>
    /// <exception cref="DomainException">The service is not offered by this provider.</exception>
    public void WithdrawService(int serviceId) => _offerings.Remove(RequireOffering(serviceId));

    private ServiceOffering RequireOffering(int serviceId) =>
        _offerings.Find(offering => offering.ServiceId == serviceId)
        ?? throw new DomainException($"This provider does not offer service {serviceId}.");
}
