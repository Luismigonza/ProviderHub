using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ProviderHub.Application.Abstractions.Events;
using ProviderHub.Domain.Common;
using ProviderHub.Domain.Common.ValueObjects;
using ProviderHub.Domain.Providers;
using ProviderHub.Domain.Providers.Events;
using ProviderHub.Domain.Providers.ValueObjects;
using ProviderHub.Domain.Services;
using ProviderHub.Infrastructure.Events;
using ProviderHub.Infrastructure.Persistence;

namespace ProviderHub.Infrastructure.Tests;

[Collection(SharedDatabase.Name)]
public class UnitOfWorkTests(DatabaseFixture database)
{
    [RequiresDatabaseFact]
    public async Task Committing_publishes_what_the_domain_recorded()
    {
        await using var context = database.CreateContext();
        var dispatcher = new RecordingDispatcher();
        var unitOfWork = new UnitOfWork(context, dispatcher);

        var service = Service.Create(Unique("Latency negotiation"), Money.Usd(175m));
        context.Services.Add(service);
        await unitOfWork.SaveChangesAsync();

        var provider = NewProvider();
        provider.OfferService(service.Id, [CountryCode.Create("CO")]);
        context.Providers.Add(provider);

        await unitOfWork.SaveChangesAsync();

        Assert.Contains(dispatcher.Dispatched, domainEvent => domainEvent is ServiceOfferedDomainEvent);
    }

    [RequiresDatabaseFact]
    public async Task Events_are_published_only_after_the_change_is_committed()
    {
        // This is the whole reason the dispatch is not inside SaveChanges. A handler that sends
        // an e-mail announces something as true; if the transaction were still open, a rollback
        // would turn that announcement into a lie, and there is no way to un-send a message.
        await using var context = database.CreateContext();

        var service = Service.Create(Unique("Firmware exorcism"), Money.Usd(260m));
        context.Services.Add(service);
        await new UnitOfWork(context, new RecordingDispatcher()).SaveChangesAsync();

        var provider = NewProvider();
        provider.OfferService(service.Id, [CountryCode.Create("CO")]);
        context.Providers.Add(provider);

        var visibleToTheHandler = -1;

        // The dispatcher looks at the database through a separate connection, so it can only see
        // rows that are actually committed.
        var observer = new CallbackDispatcher(async () =>
        {
            await using var reader = database.CreateContext();
            visibleToTheHandler = await reader.Providers.CountAsync(candidate => candidate.Id == provider.Id);
        });

        await new UnitOfWork(context, observer).SaveChangesAsync();

        Assert.Equal(1, visibleToTheHandler);
    }

    [RequiresDatabaseFact]
    public async Task Published_events_are_not_published_again_on_the_next_commit()
    {
        await using var context = database.CreateContext();
        var dispatcher = new RecordingDispatcher();
        var unitOfWork = new UnitOfWork(context, dispatcher);

        var service = Service.Create(Unique("Entropy auditing"), Money.Usd(199.99m));
        context.Services.Add(service);
        await unitOfWork.SaveChangesAsync();

        var provider = NewProvider();
        provider.OfferService(service.Id, [CountryCode.Create("CO")]);
        context.Providers.Add(provider);
        await unitOfWork.SaveChangesAsync();

        provider.Rename("Renamed afterwards");
        await unitOfWork.SaveChangesAsync();

        Assert.Single(dispatcher.Dispatched);
        Assert.Empty(provider.DomainEvents);
    }

    private static Provider NewProvider() => Provider.Create(
        UniqueNit(),
        "Importaciones Tekus S.A.",
        WebsiteUrl.Create("https://tekus.co"),
        EmailAddress.Create($"contact{Guid.NewGuid():N}@tekus.co"));

    /// <summary>
    /// A different, still valid, tax identifier per test. Tax identifiers are unique in the
    /// database, and every test in this collection shares one, so reusing a fixed value makes a
    /// test pass alone and fail beside its neighbours.
    /// </summary>
    private static Nit UniqueNit()
    {
        var baseNumber = Random.Shared
            .NextInt64(100_000_000, 999_999_999)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);

        return Nit.Create($"{baseNumber}-{Nit.CalculateCheckDigit(baseNumber)}");
    }

    private static string Unique(string name) => $"{name} {Guid.NewGuid():N}";

    private sealed class RecordingDispatcher : IDomainEventDispatcher
    {
        private readonly List<IDomainEvent> _dispatched = [];

        public IReadOnlyList<IDomainEvent> Dispatched => _dispatched;

        public Task DispatchAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken = default)
        {
            _dispatched.AddRange(domainEvents);

            return Task.CompletedTask;
        }
    }

    private sealed class CallbackDispatcher(Func<Task> onDispatch) : IDomainEventDispatcher
    {
        public async Task DispatchAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken = default) => await onDispatch();
    }
}

/// <summary>No database involved: this is about wiring handlers to events.</summary>
public class DomainEventDispatcherTests
{
    private static readonly ServiceOfferedDomainEvent AnEvent = new(
        Provider.Create(
            Nit.Create("890903938-8"),
            "Importaciones Tekus S.A.",
            WebsiteUrl.Create("https://tekus.co"),
            EmailAddress.Create("contact@tekus.co")),
        1,
        [CountryCode.Create("CO")]);

    [Fact]
    public async Task Every_handler_registered_for_an_event_is_run()
    {
        var first = new CountingHandler();
        var second = new CountingHandler();

        await Dispatcher(services =>
        {
            services.AddSingleton<IDomainEventHandler<ServiceOfferedDomainEvent>>(first);
            services.AddSingleton<IDomainEventHandler<ServiceOfferedDomainEvent>>(second);
        }).DispatchAsync([AnEvent]);

        Assert.Equal(1, first.Calls);
        Assert.Equal(1, second.Calls);
    }

    [Fact]
    public async Task An_event_nobody_handles_is_not_an_error()
    {
        await Dispatcher(_ => { }).DispatchAsync([AnEvent]);
    }

    [Fact]
    public async Task A_failing_handler_does_not_stop_the_others_and_does_not_surface()
    {
        // By the time handlers run, the transaction is committed. A broken mail server must not
        // fail a request the caller was entitled to have accepted, nor rob the other handlers of
        // their turn. The failure is logged instead.
        var survivor = new CountingHandler();

        await Dispatcher(services =>
        {
            services.AddSingleton<IDomainEventHandler<ServiceOfferedDomainEvent>>(new ThrowingHandler());
            services.AddSingleton<IDomainEventHandler<ServiceOfferedDomainEvent>>(survivor);
        }).DispatchAsync([AnEvent]);

        Assert.Equal(1, survivor.Calls);
    }

    private static DomainEventDispatcher Dispatcher(Action<IServiceCollection> register)
    {
        var services = new ServiceCollection();
        register(services);

        return new DomainEventDispatcher(
            services.BuildServiceProvider(),
            NullLogger<DomainEventDispatcher>.Instance);
    }

    private sealed class CountingHandler : IDomainEventHandler<ServiceOfferedDomainEvent>
    {
        public int Calls { get; private set; }

        public Task HandleAsync(ServiceOfferedDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            Calls++;

            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler : IDomainEventHandler<ServiceOfferedDomainEvent>
    {
        public Task HandleAsync(ServiceOfferedDomainEvent domainEvent, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The mail server is down.");
    }
}
