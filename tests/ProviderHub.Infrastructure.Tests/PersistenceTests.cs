using Microsoft.EntityFrameworkCore;
using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Application.Common;
using ProviderHub.Domain.Common.ValueObjects;
using ProviderHub.Domain.Providers;
using ProviderHub.Domain.Providers.ValueObjects;
using ProviderHub.Domain.Services;
using ProviderHub.Infrastructure.Persistence;
using ProviderHub.Infrastructure.Persistence.Repositories;

namespace ProviderHub.Infrastructure.Tests;

[Collection(SharedDatabase.Name)]
public class PersistenceTests(DatabaseFixture database)
{
    [RequiresDatabaseFact]
    public async Task An_aggregate_survives_a_round_trip_through_the_database()
    {
        int providerId;

        await using (var context = database.CreateContext())
        {
            var service = Service.Create(Unique("Space content download"), Money.Usd(120.50m));
            context.Services.Add(service);
            await context.SaveChangesAsync();

            var provider = Provider.Create(
                Nit.Create("890903938-8"),
                "Importaciones Tekus S.A.",
                WebsiteUrl.Create("https://tekus.co"),
                EmailAddress.Create("contact@tekus.co"));

            provider.OfferService(service.Id, [CountryCode.Create("CO"), CountryCode.Create("MX")]);

            context.Providers.Add(provider);
            await context.SaveChangesAsync();

            providerId = provider.Id;
            Assert.True(providerId > 0, "The database should have handed out an identity.");
        }

        // A different context, so nothing can be served from the change tracker.
        await using var reader = database.CreateContext();

        var reloaded = await reader.Providers
            .Include(provider => provider.Offerings)
            .FirstAsync(provider => provider.Id == providerId);

        Assert.Equal("890903938-8", reloaded.Nit.Value);
        Assert.Equal(8, reloaded.Nit.CheckDigit);
        Assert.Equal("contact@tekus.co", reloaded.Email.Value);
        Assert.Equal("https://tekus.co/", reloaded.Website.Value);

        var offering = Assert.Single(reloaded.Offerings);
        Assert.Equal(["CO", "MX"], offering.Countries.Select(country => country.Value).Order());

        // The countries came back as value objects, not as strings: the mapping preserved the
        // model rather than flattening it.
        Assert.Equal("Colombia", offering.Countries.First(country => country.Value == "CO").DisplayName);
    }

    [RequiresDatabaseFact]
    public async Task Money_keeps_its_cents_and_its_currency()
    {
        int serviceId;

        await using (var context = database.CreateContext())
        {
            var service = Service.Create(Unique("Quantum cache warming"), Money.Usd(210.05m));
            context.Services.Add(service);
            await context.SaveChangesAsync();
            serviceId = service.Id;
        }

        await using var reader = database.CreateContext();
        var reloaded = await reader.Services.FirstAsync(service => service.Id == serviceId);

        Assert.Equal(210.05m, reloaded.HourlyRate.Amount);
        Assert.Equal("USD", reloaded.HourlyRate.Currency);
    }

    [RequiresDatabaseFact]
    public async Task Two_providers_cannot_share_a_tax_identifier()
    {
        await using var context = database.CreateContext();

        context.Providers.Add(NewProvider("900373115-3", "First company"));
        await context.SaveChangesAsync();

        context.Providers.Add(NewProvider("900373115-3", "Second company"));

        // The use case checks this first, but two simultaneous requests can both pass that check.
        // The unique index is what actually makes the rule true.
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [RequiresDatabaseFact]
    public async Task A_provider_cannot_offer_the_same_service_twice_even_from_two_instances()
    {
        await using var context = database.CreateContext();

        var service = Service.Create(Unique("Latency negotiation"), Money.Usd(175m));
        context.Services.Add(service);

        // Saved first on purpose: until the database hands out an identity, there is no service
        // to point at, and the aggregate refuses to record an offering that references nothing.
        await context.SaveChangesAsync();

        var provider = NewProvider("811021363-0", "Bits del Caribe Ltda.");
        provider.OfferService(service.Id, [CountryCode.Create("CO")]);
        context.Providers.Add(provider);
        await context.SaveChangesAsync();

        // Reaching around the aggregate, the way a second process would.
        await using var other = database.CreateContext();
        var sameProvider = await other.Providers.FirstAsync(candidate => candidate.Id == provider.Id);
        sameProvider.OfferService(service.Id, [CountryCode.Create("MX")]);

        await Assert.ThrowsAsync<DbUpdateException>(() => other.SaveChangesAsync());
    }

    [RequiresDatabaseFact]
    public async Task A_service_that_providers_still_offer_cannot_be_deleted()
    {
        int serviceId;

        await using (var context = database.CreateContext())
        {
            var service = Service.Create(Unique("Cold storage archival"), Money.Usd(45.25m));
            context.Services.Add(service);
            await context.SaveChangesAsync();

            var provider = NewProvider("830045781-9", "Cordillera Software Group");
            provider.OfferService(service.Id, [CountryCode.Create("CO")]);
            context.Providers.Add(provider);
            await context.SaveChangesAsync();

            serviceId = service.Id;
        }

        // A fresh context that has never seen the offerings, so the delete really is sent to the
        // server. With them loaded, the change tracker refuses first and the database is never
        // asked; what is being checked here is the foreign key itself.
        await using var deleter = database.CreateContext();
        var orphaned = await deleter.Services.FirstAsync(candidate => candidate.Id == serviceId);
        deleter.Services.Remove(orphaned);

        // Restrict, not cascade: deleting a catalogue entry must not silently erase the fact
        // that providers were offering it.
        await Assert.ThrowsAsync<DbUpdateException>(() => deleter.SaveChangesAsync());
    }

    private static Provider NewProvider(string nit, string name) => Provider.Create(
        Nit.Create(nit),
        name,
        WebsiteUrl.Create("https://example.com"),
        EmailAddress.Create($"contact{Guid.NewGuid():N}@example.com"));

    /// <summary>Service names are unique, so tests must not fight over them.</summary>
    private static string Unique(string name) => $"{name} {Guid.NewGuid():N}"[..Math.Min(200, name.Length + 33)];
}

[Collection(SharedDatabase.Name)]
public class SearchAndSortingTests(DatabaseFixture database)
{
    [RequiresDatabaseFact]
    public async Task Providers_can_be_found_by_name_by_tax_id_and_by_e_mail()
    {
        await using var context = database.CreateContext();
        var repository = new ProviderRepository(context);

        context.Providers.AddRange(
            Provider.Create(
                Nit.Create("860512336-7"),
                "Pacifico Data Works",
                WebsiteUrl.Create("https://pacificodata.co"),
                EmailAddress.Create("contact@pacificodata.co")),
            Provider.Create(
                Nit.Create("901234567-7"),
                "Magdalena Systems",
                WebsiteUrl.Create("https://magdalena.co"),
                EmailAddress.Create("support@magdalena.co")));

        await context.SaveChangesAsync();

        var byName = await repository.SearchAsync(new PageRequest { Search = "pacifico" });
        var byNit = await repository.SearchAsync(new PageRequest { Search = "9012345" });
        var byEmail = await repository.SearchAsync(new PageRequest { Search = "support@" });

        Assert.Equal("Pacifico Data Works", Assert.Single(byName.Items).Name);
        Assert.Equal("Magdalena Systems", Assert.Single(byNit.Items).Name);
        Assert.Equal("Magdalena Systems", Assert.Single(byEmail.Items).Name);
    }

    [RequiresDatabaseFact]
    public async Task A_search_term_made_of_wildcards_matches_nothing()
    {
        await using var context = database.CreateContext();
        var repository = new ProviderRepository(context);

        context.Providers.Add(Provider.Create(
            Nit.Create("805019876-9"),
            "Valle Digital Partners",
            WebsiteUrl.Create("https://valledigital.com"),
            EmailAddress.Create("hello@valledigital.com")));

        await context.SaveChangesAsync();

        // Unescaped, "%" would return every row in the table.
        var result = await repository.SearchAsync(new PageRequest { Search = "%" });

        Assert.Empty(result.Items);
    }

    [RequiresDatabaseFact]
    public async Task Services_are_sorted_by_the_requested_field_and_direction()
    {
        await using var context = database.CreateContext();
        var repository = new ServiceRepository(context);

        var marker = Guid.NewGuid().ToString("N")[..8];
        context.Services.AddRange(
            Service.Create($"{marker} cheap", Money.Usd(10m)),
            Service.Create($"{marker} expensive", Money.Usd(900m)),
            Service.Create($"{marker} average", Money.Usd(100m)));

        await context.SaveChangesAsync();

        var request = new PageRequest
        {
            Search = marker,
            SortBy = ServiceSortFields.HourlyRate,
            Direction = SortDirection.Descending,
        };

        var result = await repository.SearchAsync(request);

        Assert.Equal([900m, 100m, 10m], result.Items.Select(service => service.HourlyRate.Amount));
    }

    [RequiresDatabaseFact]
    public async Task Paging_never_shows_the_same_row_twice_when_the_sort_key_repeats()
    {
        await using var context = database.CreateContext();
        var repository = new ServiceRepository(context);

        var marker = Guid.NewGuid().ToString("N")[..8];
        for (var i = 0; i < 6; i++)
        {
            // Every row shares the same rate, so the sort key alone cannot order them.
            context.Services.Add(Service.Create($"{marker} service {i}", Money.Usd(50m)));
        }

        await context.SaveChangesAsync();

        var first = await repository.SearchAsync(new PageRequest
        {
            Search = marker,
            PageSize = 3,
            Page = 1,
            SortBy = ServiceSortFields.HourlyRate,
        });

        var second = await repository.SearchAsync(new PageRequest
        {
            Search = marker,
            PageSize = 3,
            Page = 2,
            SortBy = ServiceSortFields.HourlyRate,
        });

        Assert.Equal(6, first.TotalCount);
        Assert.Equal(2, first.TotalPages);

        // The tiebreaker on the identifier is what makes this hold.
        Assert.Empty(first.Items.Select(service => service.Id)
            .Intersect(second.Items.Select(service => service.Id)));
    }
}
