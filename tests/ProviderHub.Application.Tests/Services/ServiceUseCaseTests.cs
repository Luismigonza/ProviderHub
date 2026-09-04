using FluentValidation;
using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Application.Common;
using ProviderHub.Application.Services.UseCases;
using ProviderHub.Application.Tests.TestDoubles;
using ProviderHub.Domain.Common.ValueObjects;
using ProviderHub.Domain.Services;

namespace ProviderHub.Application.Tests.Services;

public class CreateServiceHandlerTests
{
    private readonly InMemoryServiceRepository _services = new();
    private readonly RecordingUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task A_service_is_added_to_the_catalogue_and_committed()
    {
        var result = await Handler().HandleAsync(new CreateServiceCommand("Space content download", 120m));

        Assert.Equal("Space content download", result.Name);
        Assert.Equal(120m, result.HourlyRate);
        Assert.Equal("USD", result.Currency);
        Assert.Single(_services.Items);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task A_duplicated_name_is_a_conflict()
    {
        _services.Seed(Service.Create("Space content download", Money.Usd(120m)));

        await Assert.ThrowsAsync<ConflictException>(() =>
            Handler().HandleAsync(new CreateServiceCommand("space CONTENT download", 90m)));

        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Theory]
    [InlineData("", 120)]
    [InlineData("   ", 120)]
    [InlineData("Valid name", -1)]
    [InlineData("Valid name", 10.005)]
    public async Task Invalid_input_never_reaches_the_domain(string name, decimal hourlyRate)
    {
        // The command is rejected before any entity is built, so the caller gets every problem
        // at once rather than the first exception the model happened to throw.
        await Assert.ThrowsAsync<ValidationException>(() =>
            Handler().HandleAsync(new CreateServiceCommand(name, hourlyRate)));

        Assert.Empty(_services.Items);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task The_validation_error_carries_the_message_written_in_the_domain()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            Handler().HandleAsync(new CreateServiceCommand("Valid name", -5m)));

        Assert.Contains(
            exception.Errors,
            error => error.ErrorMessage.Contains("negative", StringComparison.OrdinalIgnoreCase));
    }

    private CreateServiceHandler Handler() =>
        new(_services, _unitOfWork, new CreateServiceValidator());
}

public class UpdateServiceHandlerTests
{
    private readonly InMemoryServiceRepository _services = new();
    private readonly RecordingUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task An_existing_service_is_renamed_and_repriced()
    {
        var service = _services.Seed(Service.Create("Space content download", Money.Usd(120m)));

        var result = await Handler().HandleAsync(
            new UpdateServiceCommand(service.Id, "Orbital content download", 150.75m));

        Assert.Equal("Orbital content download", result.Name);
        Assert.Equal(150.75m, result.HourlyRate);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task An_unknown_service_is_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Handler().HandleAsync(new UpdateServiceCommand(404, "Whatever", 10m)));
    }

    [Fact]
    public async Task Keeping_its_own_name_is_not_a_duplicate()
    {
        var service = _services.Seed(Service.Create("Space content download", Money.Usd(120m)));

        var result = await Handler().HandleAsync(
            new UpdateServiceCommand(service.Id, "Space content download", 200m));

        Assert.Equal(200m, result.HourlyRate);
    }

    [Fact]
    public async Task Taking_the_name_of_another_service_is_a_conflict()
    {
        _services.Seed(Service.Create("Space content download", Money.Usd(120m)));
        var second = _services.Seed(Service.Create("Forced byte disappearance", Money.Usd(80m)));

        await Assert.ThrowsAsync<ConflictException>(() =>
            Handler().HandleAsync(new UpdateServiceCommand(second.Id, "Space content download", 80m)));
    }

    private UpdateServiceHandler Handler() =>
        new(_services, _unitOfWork, new UpdateServiceValidator());
}

public class GetServicesHandlerTests
{
    private readonly InMemoryServiceRepository _services = new();

    [Fact]
    public async Task A_page_of_services_is_returned_with_its_metadata()
    {
        for (var i = 1; i <= 25; i++)
        {
            _services.Seed(Service.Create($"Service {i:00}", Money.Usd(i)));
        }

        var result = await Handler().HandleAsync(
            new GetServicesQuery(new PageRequest { Page = 2, PageSize = 10 }));

        Assert.Equal(10, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
    }

    [Theory]
    [InlineData(0, 10, null)]
    [InlineData(1, 0, null)]
    [InlineData(1, PageRequest.MaxPageSize + 1, null)]
    [InlineData(1, 10, "; DROP TABLE Services")]
    [InlineData(1, 10, "unknownField")]
    public async Task Abusive_or_unknown_paging_options_are_rejected(int page, int pageSize, string? sortBy)
    {
        // An unbounded page size is a denial of service, and an unchecked sort field is an
        // injection. Both are stopped here, before the request reaches the database.
        var query = new GetServicesQuery(new PageRequest { Page = page, PageSize = pageSize, SortBy = sortBy });

        await Assert.ThrowsAsync<ValidationException>(() => Handler().HandleAsync(query));
    }

    [Fact]
    public async Task A_known_sort_field_is_accepted()
    {
        var query = new GetServicesQuery(new PageRequest { SortBy = ServiceSortFields.HourlyRate });

        var result = await Handler().HandleAsync(query);

        Assert.Empty(result.Items);
    }

    private GetServicesHandler Handler() => new(_services, new GetServicesValidator());
}
