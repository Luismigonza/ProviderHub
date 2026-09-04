namespace ProviderHub.Domain.Common;

/// <summary>
/// Base class for domain entities: objects whose <b>identity</b> defines equality, not their
/// attribute values. Two providers that share a name are still two different providers, and a
/// provider that changes its name is still the same provider.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Unique, store-generated identifier. It stays at <c>0</c> until the entity is persisted
    /// for the first time; see <see cref="IsTransient"/>.
    /// </summary>
    public int Id { get; protected set; }

    /// <summary>
    /// Indicates that the entity has never been persisted and therefore has no identity yet.
    /// </summary>
    public bool IsTransient => Id == 0;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other || GetType() != other.GetType())
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // Two entities that have not been persisted yet cannot be compared by identity:
        // they would all look equal to each other because they all carry Id 0.
        return !IsTransient && !other.IsTransient && Id == other.Id;
    }

    public override int GetHashCode() =>
        IsTransient ? base.GetHashCode() : HashCode.Combine(GetType(), Id);
}
