using ProviderHub.Domain.Common;

namespace ProviderHub.Application.Tests.TestDoubles;

/// <summary>
/// Assigns identifiers the way a database identity column would.
/// <para>
/// <see cref="Entity.Id"/> is deliberately not settable from outside the domain, because in
/// production only the store gets to hand out identities. The in-memory repositories are playing
/// the part of that store, so they reach the setter through reflection. It is contained in this
/// one place, and it is the price of a domain that does not expose a seam it does not need.
/// </para>
/// </summary>
internal static class TestIdentity
{
    private static readonly System.Reflection.MethodInfo IdSetter =
        typeof(Entity).GetProperty(nameof(Entity.Id))!.GetSetMethod(nonPublic: true)!;

    public static T Assign<T>(T entity, int id)
        where T : Entity
    {
        IdSetter.Invoke(entity, [id]);

        return entity;
    }
}
