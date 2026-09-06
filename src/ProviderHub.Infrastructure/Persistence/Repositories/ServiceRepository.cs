using Microsoft.EntityFrameworkCore;
using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Application.Common;
using ProviderHub.Domain.Services;

namespace ProviderHub.Infrastructure.Persistence.Repositories;

/// <summary>The Entity Framework side of <see cref="IServiceRepository"/>.</summary>
internal sealed class ServiceRepository(ProviderHubDbContext context) : IServiceRepository
{
    public async Task<Service?> FindAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Services
            .FirstOrDefaultAsync(service => service.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Services
            .AnyAsync(service => service.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public async Task<bool> NameExistsAsync(
        string name,
        int? excludedServiceId = null,
        CancellationToken cancellationToken = default)
    {
        var trimmed = name?.Trim() ?? string.Empty;

        // SQL Server compares strings case-insensitively under the default collation, which is
        // what the uniqueness rule wants: "Space content download" and "SPACE CONTENT DOWNLOAD"
        // are the same catalogue entry.
        var query = context.Services.Where(service => service.Name == trimmed);

        if (excludedServiceId is { } excluded)
        {
            query = query.Where(service => service.Id != excluded);
        }

        return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Service>> GetManyAsync(
        IReadOnlyCollection<int> serviceIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceIds);

        if (serviceIds.Count == 0)
        {
            return [];
        }

        return await context.Services
            .Where(service => serviceIds.Contains(service.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PagedResult<Service>> SearchAsync(
        PageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = context.Services.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{SearchPattern.Escape(request.Search)}%";

            query = query.Where(service => EF.Functions.Like(service.Name, pattern));
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await Sort(query, request)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<Service>(items, request.Page, request.PageSize, totalCount);
    }

    public void Add(Service service) => context.Services.Add(service);

    public void Remove(Service service) => context.Services.Remove(service);

    private static IQueryable<Service> Sort(IQueryable<Service> query, PageRequest request)
    {
        var descending = request.Direction == SortDirection.Descending;

        if (string.Equals(request.SortBy, ServiceSortFields.HourlyRate, StringComparison.OrdinalIgnoreCase))
        {
            return descending
                ? query.OrderByDescending(service => service.HourlyRate.Amount).ThenBy(service => service.Id)
                : query.OrderBy(service => service.HourlyRate.Amount).ThenBy(service => service.Id);
        }

        return descending
            ? query.OrderByDescending(service => service.Name).ThenBy(service => service.Id)
            : query.OrderBy(service => service.Name).ThenBy(service => service.Id);
    }
}
