using FluentValidation;
using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Application.Common;
using ProviderHub.Application.Services.Contracts;

namespace ProviderHub.Application.Services.UseCases;

/// <summary>Lists the catalogue with paging, searching and sorting.</summary>
public sealed record GetServicesQuery(PageRequest Page);

public sealed class GetServicesValidator : AbstractValidator<GetServicesQuery>
{
    public GetServicesValidator() =>
        RuleFor(query => query.Page)
            .NotNull()
            .SetValidator(new PageRequestValidator(ServiceSortFields.All));
}

public sealed class GetServicesHandler(
    IServiceRepository services,
    IValidator<GetServicesQuery> validator)
{
    public async Task<PagedResult<ServiceDto>> HandleAsync(
        GetServicesQuery query,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(query, cancellationToken).ConfigureAwait(false);

        var page = await services.SearchAsync(query.Page, cancellationToken).ConfigureAwait(false);

        // Map preserves the paging metadata, so the page is not rebuilt by hand here.
        return page.Map(ServiceDto.From);
    }
}
