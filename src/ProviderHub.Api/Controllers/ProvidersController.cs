using Microsoft.AspNetCore.Mvc;
using ProviderHub.Api.Contracts;
using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Application.Common;
using ProviderHub.Application.Providers.Contracts;
using ProviderHub.Application.Providers.UseCases;

namespace ProviderHub.Api.Controllers;

/// <summary>
/// Providers and the services they offer.
/// <para>
/// Every action does the same three things: turn the HTTP request into a command, hand it to a
/// use case, and turn the answer into a status code. There is no business logic here, which is
/// what makes the controllers short and the use cases testable without a web server.
/// </para>
/// </summary>
[ApiController]
[Route("api/providers")]
[Produces("application/json")]
public sealed class ProvidersController : ControllerBase
{
    private const string RouteName = "GetProviderById";

    /// <summary>
    /// Lists providers, with paging, searching and sorting. Sorting accepts <c>name</c>,
    /// <c>nit</c> or <c>email</c>; searching looks into the name, the tax identifier and the
    /// e-mail address.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ProviderListItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ProviderListItemDto>>> List(
        [FromServices] GetProvidersHandler handler,
        [FromQuery] ListQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var result = await handler
            .HandleAsync(new GetProvidersQuery(query.ToPageRequest()), cancellationToken)
            .ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>Reads one provider with the detail of everything it offers.</summary>
    // Route names are global to the application, not scoped to a controller, so two
    // actions called GetById would collide at startup.
    [HttpGet("{id:int}", Name = RouteName)]
    [ProducesResponseType<ProviderDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProviderDetailsDto>> GetById(
        [FromServices] GetProviderByIdHandler handler,
        int id,
        CancellationToken cancellationToken)
    {
        var provider = await handler
            .HandleAsync(new GetProviderByIdQuery(id), cancellationToken)
            .ConfigureAwait(false);

        return Ok(provider);
    }

    /// <summary>Registers a provider. Services are attached afterwards, one offering at a time.</summary>
    [HttpPost]
    [ProducesResponseType<ProviderDetailsDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProviderDetailsDto>> Create(
        [FromServices] CreateProviderHandler handler,
        [FromBody] SaveProviderRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new CreateProviderCommand(request.Nit, request.Name, request.Website, request.Email);
        var provider = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        // 201 with the address of the new resource, so a client never has to guess the URL.
        return CreatedAtRoute(RouteName, new { id = provider.Id }, provider);
    }

    /// <summary>Edits the identifying and contact details of a provider.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<ProviderDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProviderDetailsDto>> Update(
        [FromServices] UpdateProviderHandler handler,
        int id,
        [FromBody] SaveProviderRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // The identifier comes from the route, never from the body.
        var command = new UpdateProviderCommand(id, request.Nit, request.Name, request.Website, request.Email);

        return Ok(await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Enables a catalogue service for this provider in a set of countries.</summary>
    [HttpPost("{id:int}/services")]
    [ProducesResponseType<ProviderDetailsDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProviderDetailsDto>> OfferService(
        [FromServices] OfferServiceHandler handler,
        int id,
        [FromBody] OfferServiceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new OfferServiceCommand(id, request.ServiceId, request.Countries ?? []);
        var provider = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        return CreatedAtRoute(RouteName, new { id = provider.Id }, provider);
    }

    /// <summary>Replaces the countries where an already offered service is available.</summary>
    [HttpPut("{id:int}/services/{serviceId:int}")]
    [ProducesResponseType<ProviderDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProviderDetailsDto>> ChangeOfferedCountries(
        [FromServices] ChangeOfferedCountriesHandler handler,
        int id,
        int serviceId,
        [FromBody] ChangeCountriesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new ChangeOfferedCountriesCommand(id, serviceId, request.Countries ?? []);

        return Ok(await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Stops this provider from offering a service. The catalogue entry is untouched.</summary>
    [HttpDelete("{id:int}/services/{serviceId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> WithdrawService(
        [FromServices] WithdrawServiceHandler handler,
        int id,
        int serviceId,
        CancellationToken cancellationToken)
    {
        await handler
            .HandleAsync(new WithdrawServiceCommand(id, serviceId), cancellationToken)
            .ConfigureAwait(false);

        // Nothing left to say, and nothing to send back.
        return NoContent();
    }

    /// <summary>Fields this list can be sorted by, exposed so the frontend does not hard-code them.</summary>
    [HttpGet("sort-fields")]
    [ProducesResponseType<IReadOnlyCollection<string>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyCollection<string>> SortFields() => Ok(ProviderSortFields.All);
}
