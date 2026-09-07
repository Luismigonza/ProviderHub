using System.Diagnostics.CodeAnalysis;
using ProviderHub.Domain.Common;

namespace ProviderHub.Application.Abstractions.Events;

/// <summary>
/// Reacts to something the domain recorded as having happened.
/// <para>
/// A handler runs <b>after</b> the change is committed, so what it reacts to is a fact and not
/// an intention. That ordering is the whole point: an e-mail announcing a service that a failed
/// transaction never created cannot be recalled.
/// </para>
/// </summary>
/// <typeparam name="TEvent">The event this handler cares about.</typeparam>
[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "The rule reserves the EventHandler suffix for types built on the EventHandler "
                  + "delegate. This is a domain event handler in the DDD sense, and that name is the "
                  + "one every reader of this codebase will already know. Suppressed here rather than "
                  + "disabled globally, so the rule keeps working everywhere else.")]
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Delivers recorded events to whoever handles them.
/// <para>
/// The aggregate that raised an event never learns who listened, or whether anyone did. That is
/// what lets a second reaction be added later, an audit trail or a webhook, without reopening
/// the model.
/// </para>
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default);
}
