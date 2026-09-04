using ProviderHub.Domain.Services;

namespace ProviderHub.Application.Services.Contracts;

/// <summary>
/// A service as the outside world sees it.
/// <para>
/// Domain entities never cross the boundary. If they did, every rename inside the model would
/// break the frontend, the aggregate would have to be serializable, and a private detail would
/// only need to be made public once to leak forever. A DTO is a deliberate, stable contract.
/// </para>
/// </summary>
public sealed record ServiceDto(int Id, string Name, decimal HourlyRate, string Currency)
{
    public static ServiceDto From(Service service)
    {
        ArgumentNullException.ThrowIfNull(service);

        return new ServiceDto(
            service.Id,
            service.Name,
            service.HourlyRate.Amount,
            service.HourlyRate.Currency);
    }
}
