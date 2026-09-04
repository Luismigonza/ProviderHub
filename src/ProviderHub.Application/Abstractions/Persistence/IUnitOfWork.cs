namespace ProviderHub.Application.Abstractions.Persistence;

/// <summary>
/// Commits everything a use case changed, as one atomic operation.
/// <para>
/// Repositories only stage changes; nothing reaches the database until this is called. That is
/// what makes a use case that touches two aggregates all-or-nothing, and it is also the moment
/// the recorded domain events become safe to publish: the facts they announce are, by then,
/// actually true.
/// </para>
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
