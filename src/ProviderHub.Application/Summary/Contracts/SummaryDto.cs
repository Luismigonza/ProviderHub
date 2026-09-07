namespace ProviderHub.Application.Summary.Contracts;

/// <summary>
/// One country, and the two indicators the test asks for.
/// </summary>
/// <param name="CountryCode">ISO 3166-1 alpha-2 code.</param>
/// <param name="CountryName">The country's name in English, resolved from the code.</param>
/// <param name="ServiceCount">Distinct services offered in this country.</param>
/// <param name="ProviderCount">Distinct providers offering something in this country.</param>
public sealed record CountryIndicatorDto(
    string CountryCode,
    string CountryName,
    int ServiceCount,
    int ProviderCount);

/// <summary>Headline figures, so a dashboard has something to show above the breakdown.</summary>
public sealed record SummaryTotalsDto(
    int ProviderCount,
    int ServiceCount,
    int OfferingCount,
    int CountryCount);

/// <summary>
/// The answer of the summary endpoint.
/// <para>
/// The two indicators are services per country and providers per country. They are answerable at
/// all because of a modelling decision taken at the very start: the statement of the test asks
/// for figures "by country" without ever defining a country field, and this project put it on
/// the relationship between a provider and a service. A country on the provider would only have
/// answered the second of the two.
/// </para>
/// </summary>
public sealed record SummaryDto(SummaryTotalsDto Totals, IReadOnlyList<CountryIndicatorDto> ByCountry);
