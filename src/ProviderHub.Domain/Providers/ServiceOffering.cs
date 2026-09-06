using ProviderHub.Domain.Common;
using ProviderHub.Domain.Common.ValueObjects;

namespace ProviderHub.Domain.Providers;

/// <summary>
/// The fact that a provider offers a given service in a given set of countries.
/// <para>
/// This is what the test calls "the provider relates its services", modelled as an entity of its
/// own rather than as a plain join row, because the relationship carries information: the same
/// provider may offer one service across Colombia and Mexico and another one only in Peru.
/// That is also what makes the country indicators of the summary endpoint answerable.
/// </para>
/// <para>
/// It lives inside the <see cref="Provider"/> aggregate, so it can only be created and modified
/// through the provider. That is the point of an aggregate: a single door through which every
/// change passes, which is the only way invariants can be guaranteed.
/// </para>
/// </summary>
public sealed class ServiceOffering : Entity
{
    private readonly List<CountryCode> _countries = [];

    /// <summary>Required by the persistence layer. See the note on <c>Provider</c>.</summary>
    private ServiceOffering()
    {
    }

    internal ServiceOffering(int serviceId, IEnumerable<CountryCode> countries)
    {
        if (serviceId <= 0)
        {
            throw new DomainException("A service offering must reference an existing service.");
        }

        ServiceId = serviceId;
        ReplaceCountries(countries);
    }

    /// <summary>Owning provider. Set by the persistence layer through the aggregate.</summary>
    public int ProviderId { get; private set; }

    /// <summary>The offered <see cref="Services.Service"/>.</summary>
    public int ServiceId { get; private set; }

    /// <summary>Countries where the provider offers this service. Never empty.</summary>
    public IReadOnlyCollection<CountryCode> Countries => _countries.AsReadOnly();

    internal void ChangeCountries(IEnumerable<CountryCode> countries) => ReplaceCountries(countries);

    private void ReplaceCountries(IEnumerable<CountryCode> countries)
    {
        ArgumentNullException.ThrowIfNull(countries);

        // Distinct() relies on the value semantics of CountryCode: two codes with the same
        // value are the same country, so listing "CO" twice is a harmless duplicate, not an
        // error worth rejecting the whole request for.
        var distinct = countries.Distinct().ToList();

        if (distinct.Count == 0)
        {
            throw new DomainException("A service must be offered in at least one country.");
        }

        _countries.Clear();
        _countries.AddRange(distinct);
    }
}
