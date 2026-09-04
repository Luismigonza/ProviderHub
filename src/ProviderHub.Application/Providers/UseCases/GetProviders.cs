using FluentValidation;
using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Application.Common;
using ProviderHub.Application.Providers.Contracts;

namespace ProviderHub.Application.Providers.UseCases;

/// <summary>Lists providers with paging, searching and sorting.</summary>
public sealed record GetProvidersQuery(PageRequest Page);

public sealed class GetProvidersValidator : AbstractValidator<GetProvidersQuery>
{
    public GetProvidersValidator() =>
        RuleFor(query => query.Page)
            .NotNull()
            .SetValidator(new PageRequestValidator(ProviderSortFields.All));
}

public sealed class GetProvidersHandler(
    IProviderRepository providers,
    IValidator<GetProvidersQuery> validator)
{
    public async Task<PagedResult<ProviderListItemDto>> HandleAsync(
        GetProvidersQuery query,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(query, cancellationToken).ConfigureAwait(false);

        var page = await providers.SearchAsync(query.Page, cancellationToken).ConfigureAwait(false);

        return page.Map(ProviderListItemDto.From);
    }
}
