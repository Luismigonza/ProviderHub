using FluentValidation;

namespace ProviderHub.Application.Common;

/// <summary>
/// Validates the paging, searching and sorting options shared by every list.
/// <para>
/// The set of sortable fields is supplied by each list rather than hard-coded here: it is the
/// list that knows what it can order by. Rejecting anything outside that set is not only a
/// usability decision, it is what stops an arbitrary string from reaching the database.
/// </para>
/// </summary>
public sealed class PageRequestValidator : AbstractValidator<PageRequest>
{
    public PageRequestValidator(IReadOnlyCollection<string> allowedSortFields)
    {
        ArgumentNullException.ThrowIfNull(allowedSortFields);

        RuleFor(request => request.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be 1 or greater.");

        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, PageRequest.MaxPageSize)
            .WithMessage($"Page size must be between 1 and {PageRequest.MaxPageSize}.");

        RuleFor(request => request.Search)
            .MaximumLength(200)
            .WithMessage("The search term is too long.");

        RuleFor(request => request.SortBy)
            .Must(field => field is null || allowedSortFields.Contains(field, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Sorting is only supported by: {string.Join(", ", allowedSortFields)}.");
    }
}
