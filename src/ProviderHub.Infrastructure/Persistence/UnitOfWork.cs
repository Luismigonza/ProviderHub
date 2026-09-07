using Microsoft.EntityFrameworkCore;
using ProviderHub.Application.Abstractions.Events;
using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Domain.Common;

namespace ProviderHub.Infrastructure.Persistence;

/// <summary>
/// Commits the work of a use case and then publishes what the domain recorded along the way.
/// <para>
/// The order is the entire design. Events are collected before saving, because saving clears
/// nothing on its own and the change tracker is easiest to read while the entities are still
/// attached. They are dispatched only <b>after</b> the commit succeeds, because a handler that
/// sends an e-mail is announcing something as true, and a transaction that rolls back afterwards
/// would make it a lie. There is no way to un-send a message.
/// </para>
/// <para>
/// The reverse trade-off is real and accepted: if the process dies between the commit and the
/// dispatch, the notification is lost. Making that impossible needs an outbox table written in
/// the same transaction and drained by a background worker, which is the right answer for a
/// system where a missed notification costs money. Here, a lost e-mail is cheaper than a
/// provider whose registration was rolled back after somebody was told about it.
/// </para>
/// </summary>
internal sealed class UnitOfWork(ProviderHubDbContext context, IDomainEventDispatcher dispatcher) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = context.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = aggregates.SelectMany(aggregate => aggregate.DomainEvents).ToList();

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Cleared before dispatching, so that a handler which saves again cannot set the same
        // events off a second time.
        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        if (domainEvents.Count > 0)
        {
            await dispatcher.DispatchAsync(domainEvents, cancellationToken).ConfigureAwait(false);
        }
    }
}
