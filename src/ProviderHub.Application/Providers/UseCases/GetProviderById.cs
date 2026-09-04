using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Application.Common;
using ProviderHub.Application.Providers.Contracts;

namespace ProviderHub.Application.Providers.UseCases;

/// <summary>Reads a provider together with the detail of everything it offers.</summary>
public sealed record GetProviderByIdQuery(int Id);

public sealed class GetProviderByIdHandler(
    IProviderRepository providers,
    IServiceRepository services)
{
    public async Task<ProviderDetailsDto> HandleAsync(
        GetProviderByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var provider = await providers.FindAsync(query.Id, cancellationToken).ConfigureAwait(false)
            ?? throw NotFoundException.For("Provider", query.Id);

        // Two queries, whatever the number of offerings: the aggregate knows the identifiers,
        // and the catalogue resolves all of them in one go.
        var offeredServices = await services
            .GetManyAsync([.. provider.Offerings.Select(offering => offering.ServiceId)], cancellationToken)
            .ConfigureAwait(false);

        return ProviderDetailsDto.From(provider, offeredServices);
    }
}
