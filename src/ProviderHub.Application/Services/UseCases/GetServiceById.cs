using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Application.Common;
using ProviderHub.Application.Services.Contracts;

namespace ProviderHub.Application.Services.UseCases;

/// <summary>Reads a single catalogue service.</summary>
public sealed record GetServiceByIdQuery(int Id);

public sealed class GetServiceByIdHandler(IServiceRepository services)
{
    public async Task<ServiceDto> HandleAsync(
        GetServiceByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var service = await services.FindAsync(query.Id, cancellationToken).ConfigureAwait(false)
            ?? throw NotFoundException.For("Service", query.Id);

        return ServiceDto.From(service);
    }
}
