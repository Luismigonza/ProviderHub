using ProviderHub.Domain.Providers;
using ProviderHub.Domain.Services;

namespace ProviderHub.Application.Providers.Contracts;

/// <summary>A country, sent with its display name so the frontend does not need a lookup table.</summary>
public sealed record CountryDto(string Code, string Name);

/// <summary>One service a provider offers, and where.</summary>
public sealed record ServiceOfferingDto(
    int ServiceId,
    string ServiceName,
    decimal HourlyRate,
    string Currency,
    IReadOnlyList<CountryDto> Countries);

/// <summary>
/// A provider as it appears in a list: enough to render a row, without the cost of loading and
/// serializing every offering in detail.
/// </summary>
public sealed record ProviderListItemDto(
    int Id,
    string Nit,
    string Name,
    string Website,
    string Email,
    int OfferedServiceCount,
    IReadOnlyList<CountryDto> Countries)
{
    public static ProviderListItemDto From(Provider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        var countries = provider.Offerings
            .SelectMany(offering => offering.Countries)
            .Distinct()
            .OrderBy(country => country.Value, StringComparer.Ordinal)
            .Select(country => new CountryDto(country.Value, country.DisplayName))
            .ToList();

        return new ProviderListItemDto(
            provider.Id,
            provider.Nit.Value,
            provider.Name,
            provider.Website.Value,
            provider.Email.Value,
            provider.Offerings.Count,
            countries);
    }
}

/// <summary>A provider with the full detail of what it offers.</summary>
public sealed record ProviderDetailsDto(
    int Id,
    string Nit,
    string Name,
    string Website,
    string Email,
    IReadOnlyList<ServiceOfferingDto> Offerings)
{
    /// <summary>
    /// Builds the detail view. The services are passed in rather than looked up one by one,
    /// because the provider aggregate only knows their identifiers: an aggregate references
    /// another aggregate by id, it does not hold it.
    /// </summary>
    public static ProviderDetailsDto From(Provider provider, IReadOnlyList<Service> offeredServices)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(offeredServices);

        var servicesById = offeredServices.ToDictionary(service => service.Id);

        var offerings = provider.Offerings
            .Select(offering =>
            {
                var service = servicesById.GetValueOrDefault(offering.ServiceId);

                return new ServiceOfferingDto(
                    offering.ServiceId,
                    service?.Name ?? string.Empty,
                    service?.HourlyRate.Amount ?? 0m,
                    service?.HourlyRate.Currency ?? string.Empty,
                    [.. offering.Countries
                        .OrderBy(country => country.Value, StringComparer.Ordinal)
                        .Select(country => new CountryDto(country.Value, country.DisplayName))]);
            })
            .OrderBy(offering => offering.ServiceName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ProviderDetailsDto(
            provider.Id,
            provider.Nit.Value,
            provider.Name,
            provider.Website.Value,
            provider.Email.Value,
            offerings);
    }
}
