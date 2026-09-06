using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProviderHub.Api.Contracts;
using ProviderHub.Application.Authentication.UseCases;

namespace ProviderHub.Api.Controllers;

/// <summary>Signing in.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    /// <summary>
    /// Exchanges the credentials of the default user for an access token. Send the token back as
    /// <c>Authorization: Bearer &lt;token&gt;</c> on every other endpoint.
    /// </summary>
    [HttpPost("login")]

    // The one endpoint that cannot require a token, since obtaining one is what it is for.
    [AllowAnonymous]
    [ProducesResponseType<AccessTokenDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AccessTokenDto>> Login(
        [FromServices] SignInHandler handler,
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await handler
            .HandleAsync(new SignInCommand(request.UserName, request.Password), cancellationToken)
            .ConfigureAwait(false);

        return Ok(token);
    }

    /// <summary>Reports who the current token belongs to. Useful for checking a token is live.</summary>
    [HttpGet("me")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<object> Me() => Ok(new { userName = User.Identity?.Name });
}
