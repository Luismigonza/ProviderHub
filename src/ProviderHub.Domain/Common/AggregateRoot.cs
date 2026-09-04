namespace ProviderHub.Domain.Common;

/// <summary>
/// Entry point of an aggregate: the only object the outside world is allowed to hold a
/// reference to, and the boundary within which every invariant must hold. Everything inside the
/// aggregate is reached through its root, which is why <see cref="Providers.ServiceOffering"/>
/// can only be created by <see cref="Providers.Provider"/>.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Facts recorded by this aggregate that have not been dispatched yet. They are published
    /// by the infrastructure once the changes are safely committed, never before: an e-mail
    /// cannot be un-sent if the transaction ends up rolling back.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Called by the infrastructure after the recorded events have been dispatched.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
