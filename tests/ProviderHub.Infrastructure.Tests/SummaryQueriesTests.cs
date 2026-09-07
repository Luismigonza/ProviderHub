using System.Globalization;
using ProviderHub.Domain.Common.ValueObjects;
using ProviderHub.Domain.Providers;
using ProviderHub.Domain.Providers.ValueObjects;
using ProviderHub.Domain.Services;
using ProviderHub.Infrastructure.Persistence.Repositories;

namespace ProviderHub.Infrastructure.Tests;

/// <summary>
/// The indicators are counted by SQL Server, so they are checked against SQL Server. Counting in
/// memory in a test would prove the arithmetic and nothing about the query that actually runs.
/// </summary>
[Collection(SharedDatabase.Name)]
public class SummaryQueriesTests(DatabaseFixture database)
{
    [RequiresDatabaseFact]
    public async Task A_provider_offering_three_services_in_one_country_counts_once_there()
    {
        await using var context = database.CreateContext();

        var first = context.Services.Add(Service.Create(Unique("First"), Money.Usd(10m))).Entity;
        var second = context.Services.Add(Service.Create(Unique("Second"), Money.Usd(20m))).Entity;
        var third = context.Services.Add(Service.Create(Unique("Third"), Money.Usd(30m))).Entity;
        await context.SaveChangesAsync();

        var provider = NewProvider();
        provider.OfferService(first.Id, [CountryCode.Create("VU")]);
        provider.OfferService(second.Id, [CountryCode.Create("VU")]);
        provider.OfferService(third.Id, [CountryCode.Create("VU")]);
        context.Providers.Add(provider);
        await context.SaveChangesAsync();

        var summary = await new SummaryQueries(context).GetSummaryAsync();
        var vanuatu = summary.ByCountry.Single(country => country.CountryCode == "VU");

        // Three services, one provider. Without COUNT(DISTINCT) the provider would be counted
        // once per offering and the indicator would read three.
        Assert.Equal(3, vanuatu.ServiceCount);
        Assert.Equal(1, vanuatu.ProviderCount);
        Assert.Equal("Vanuatu", vanuatu.CountryName);
    }

    [RequiresDatabaseFact]
    public async Task Two_providers_offering_the_same_service_count_as_one_service_there()
    {
        await using var context = database.CreateContext();

        var shared = context.Services.Add(Service.Create(Unique("Shared"), Money.Usd(50m))).Entity;
        await context.SaveChangesAsync();

        var first = NewProvider();
        first.OfferService(shared.Id, [CountryCode.Create("TO")]);
        var second = NewProvider();
        second.OfferService(shared.Id, [CountryCode.Create("TO")]);
        context.Providers.AddRange(first, second);
        await context.SaveChangesAsync();

        var summary = await new SummaryQueries(context).GetSummaryAsync();
        var tonga = summary.ByCountry.Single(country => country.CountryCode == "TO");

        Assert.Equal(1, tonga.ServiceCount);
        Assert.Equal(2, tonga.ProviderCount);
    }

    [RequiresDatabaseFact]
    public async Task An_offering_spanning_countries_is_counted_in_each_of_them()
    {
        await using var context = database.CreateContext();

        var service = context.Services.Add(Service.Create(Unique("Spanning"), Money.Usd(75m))).Entity;
        await context.SaveChangesAsync();

        var provider = NewProvider();
        provider.OfferService(service.Id, [CountryCode.Create("FJ"), CountryCode.Create("WS")]);
        context.Providers.Add(provider);
        await context.SaveChangesAsync();

        var summary = await new SummaryQueries(context).GetSummaryAsync();

        Assert.Equal(1, summary.ByCountry.Single(country => country.CountryCode == "FJ").ServiceCount);
        Assert.Equal(1, summary.ByCountry.Single(country => country.CountryCode == "WS").ServiceCount);
    }

    [RequiresDatabaseFact]
    public async Task A_country_nobody_offers_anything_in_does_not_appear()
    {
        await using var context = database.CreateContext();

        var summary = await new SummaryQueries(context).GetSummaryAsync();

        // The breakdown reports what exists, not every country on the map.
        Assert.DoesNotContain(summary.ByCountry, country => country.CountryCode == "AQ");
    }

    [RequiresDatabaseFact]
    public async Task Countries_are_ordered_by_how_many_services_reach_them()
    {
        await using var context = database.CreateContext();

        var summary = await new SummaryQueries(context).GetSummaryAsync();

        Assert.Equal(
            summary.ByCountry.Select(country => country.ServiceCount).OrderByDescending(count => count),
            summary.ByCountry.Select(country => country.ServiceCount));
    }

    [RequiresDatabaseFact]
    public async Task The_totals_agree_with_the_breakdown()
    {
        await using var context = database.CreateContext();

        var summary = await new SummaryQueries(context).GetSummaryAsync();

        Assert.Equal(summary.ByCountry.Count, summary.Totals.CountryCount);
        Assert.True(summary.Totals.ProviderCount > 0);
        Assert.True(summary.Totals.OfferingCount > 0);
    }

    private static Provider NewProvider() => Provider.Create(
        UniqueNit(),
        "Summary test provider",
        WebsiteUrl.Create("https://example.com"),
        EmailAddress.Create($"contact{Guid.NewGuid():N}@example.com"));

    private static Nit UniqueNit()
    {
        var baseNumber = Random.Shared.NextInt64(100_000_000, 999_999_999)
            .ToString(CultureInfo.InvariantCulture);

        return Nit.Create($"{baseNumber}-{Nit.CalculateCheckDigit(baseNumber)}");
    }

    private static string Unique(string name) => $"{name} {Guid.NewGuid():N}";
}
