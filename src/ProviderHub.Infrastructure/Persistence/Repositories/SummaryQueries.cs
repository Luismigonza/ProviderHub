using Microsoft.EntityFrameworkCore;
using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Application.Summary.Contracts;
using ProviderHub.Domain.Common.ValueObjects;

namespace ProviderHub.Infrastructure.Persistence.Repositories;

/// <summary>
/// Produces the dashboard figures with the database doing the counting.
/// </summary>
internal sealed class SummaryQueries(ProviderHubDbContext context) : ISummaryQueries
{
    public async Task<SummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        // One row per (provider, service, country), flattened from the aggregate. This is only
        // possible because the country lives on the offering: had it been a field on the
        // provider, "services per country" would have had no honest answer.
        var offeredCountries = context.Providers
            .SelectMany(
                provider => provider.Offerings,
                (provider, offering) => new { ProviderId = provider.Id, offering.ServiceId, offering.Countries })
            .SelectMany(
                offering => offering.Countries,
                (offering, country) => new { offering.ProviderId, offering.ServiceId, Country = country.Value });

        var byCountry = await offeredCountries
            .GroupBy(row => row.Country)
            .Select(group => new
            {
                Country = group.Key,

                // Distinct, because a provider offering three services in Colombia is one
                // provider there, not three.
                ServiceCount = group.Select(row => row.ServiceId).Distinct().Count(),
                ProviderCount = group.Select(row => row.ProviderId).Distinct().Count(),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var totals = new SummaryTotalsDto(
            await context.Providers.CountAsync(cancellationToken).ConfigureAwait(false),
            await context.Services.CountAsync(cancellationToken).ConfigureAwait(false),
            await context.Providers
                .SelectMany(provider => provider.Offerings)
                .CountAsync(cancellationToken)
                .ConfigureAwait(false),
            byCountry.Count);

        var indicators = byCountry
            .Select(row => new CountryIndicatorDto(
                row.Country,

                // Resolved here rather than stored: one country, one row, one spelling.
                CountryCode.Create(row.Country).DisplayName,
                row.ServiceCount,
                row.ProviderCount))
            .OrderByDescending(indicator => indicator.ServiceCount)
            .ThenBy(indicator => indicator.CountryName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SummaryDto(totals, indicators);
    }
}
