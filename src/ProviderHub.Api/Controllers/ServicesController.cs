using Microsoft.AspNetCore.Mvc;
using ProviderHub.Api.Contracts;
using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Application.Common;
using ProviderHub.Application.Services.Contracts;
using ProviderHub.Application.Services.UseCases;

namespace ProviderHub.Api.Controllers;

/// <summary>The catalogue of services providers can offer.</summary>
[ApiController]
[Route("api/services")]
[Produces("application/json")]
public sealed class ServicesController : ControllerBase
{
    private const string RouteName = "GetServiceById";

    /// <summary>
    /// Lists the catalogue, with paging, searching and sorting. Sorting accepts <c>name</c> or
    /// <c>hourlyRate</c>; searching looks into the name.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ServiceDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ServiceDto>>> List(
        [FromServices] GetServicesHandler handler,
        [FromQuery] ListQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var result = await handler
            .HandleAsync(new GetServicesQuery(query.ToPageRequest()), cancellationToken)
            .ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>Reads one catalogue service.</summary>
    [HttpGet("{id:int}", Name = RouteName)]
    [ProducesResponseType<ServiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceDto>> GetById(
        [FromServices] GetServiceByIdHandler handler,
        int id,
        CancellationToken cancellationToken)
    {
        var service = await handler
            .HandleAsync(new GetServiceByIdQuery(id), cancellationToken)
            .ConfigureAwait(false);

        return Ok(service);
    }

    /// <summary>Adds a service to the catalogue.</summary>
    [HttpPost]
    [ProducesResponseType<ServiceDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServiceDto>> Create(
        [FromServices] CreateServiceHandler handler,
        [FromBody] SaveServiceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var service = await handler
            .HandleAsync(new CreateServiceCommand(request.Name, request.HourlyRate), cancellationToken)
            .ConfigureAwait(false);

        return CreatedAtRoute(RouteName, new { id = service.Id }, service);
    }

    /// <summary>Edits the name or the hourly rate of a catalogue service.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<ServiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServiceDto>> Update(
        [FromServices] UpdateServiceHandler handler,
        int id,
        [FromBody] SaveServiceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new UpdateServiceCommand(id, request.Name, request.HourlyRate);

        return Ok(await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Fields this list can be sorted by.</summary>
    [HttpGet("sort-fields")]
    [ProducesResponseType<IReadOnlyCollection<string>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyCollection<string>> SortFields() => Ok(ServiceSortFields.All);
}
