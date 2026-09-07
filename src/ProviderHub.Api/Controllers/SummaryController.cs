using Microsoft.AspNetCore.Mvc;
using ProviderHub.Application.Summary.Contracts;
using ProviderHub.Application.Summary.UseCases;

namespace ProviderHub.Api.Controllers;

/// <summary>Figures for the dashboard.</summary>
[ApiController]
[Route("api/summary")]
[Produces("application/json")]
public sealed class SummaryController : ControllerBase
{
    /// <summary>
    /// Returns the headline totals and, per country, how many distinct services are offered
    /// there and how many providers offer something there.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<SummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SummaryDto>> Get(
        [FromServices] GetSummaryHandler handler,
        CancellationToken cancellationToken)
    {
        var summary = await handler
            .HandleAsync(new GetSummaryQuery(), cancellationToken)
            .ConfigureAwait(false);

        return Ok(summary);
    }
}
