namespace ProviderHub.Domain.Common;

/// <summary>
/// Something meaningful that already happened inside the domain, expressed in the language of
/// the business. Domain events let the model announce a fact without knowing, or caring, who
/// reacts to it: sending an e-mail, writing an audit trail, refreshing a projection.
/// </summary>
public interface IDomainEvent
{
    /// <summary>When the fact occurred.</summary>
    DateTimeOffset OccurredOn { get; }
}
