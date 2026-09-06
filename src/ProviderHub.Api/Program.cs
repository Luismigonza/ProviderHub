using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProviderHub.Api.Errors;
using ProviderHub.Application;
using ProviderHub.Infrastructure;
using ProviderHub.Infrastructure.Authentication;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// The composition root, and the only place the three layers are named together.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // The same options the token issuer uses, so the two halves cannot drift apart.
        var authentication = builder.Configuration
            .GetSection(JwtAuthenticationOptions.SectionName)
            .Get<JwtAuthenticationOptions>() ?? new JwtAuthenticationOptions();

        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Each of these is off by default in somebody's tutorial, and each one that is left
            // off turns the token into a decoration: an unvalidated signature accepts a token
            // anyone can forge, and an unvalidated lifetime accepts one that expired last year.
            ValidateIssuer = true,
            ValidIssuer = authentication.Issuer,
            ValidateAudience = true,
            ValidAudience = authentication.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authentication.SigningKey)),
            ValidateLifetime = true,

            // The default allows five minutes of drift, which quietly extends every token.
            ClockSkew = TimeSpan.FromSeconds(30),

            // Which claim answers User.Identity.Name. Left unsaid, the framework looks for a
            // claim type this token does not carry and the caller comes back anonymous even
            // though the token validated perfectly.
            NameClaimType = "name",
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers(options =>
{
    // Authentication is required by default and waived explicitly with [AllowAnonymous]. The
    // other way round, an endpoint added later is public until somebody remembers to protect it,
    // and nothing fails to remind them.
    options.Filters.Add(new AuthorizeFilter(
        new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));
});

// Every failure leaves as a ProblemDetails, whether it came from an exception or from the
// framework's own model binding.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

var app = builder.Build();

app.UseExceptionHandler();

// Interactive API documentation, generated from the OpenAPI document and the XML comments in the
// controllers. Development only: the shape of an API is not something to publish by accident.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("ProviderHub API"));
}

app.UseHttpsRedirection();

// Order matters: authentication works out who the caller is, authorization decides whether that
// caller may proceed. Reversed, there is nobody to authorize yet.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();

/// <summary>
/// Named so that the integration tests can boot this exact application through
/// <c>WebApplicationFactory</c>. Top-level statements generate an internal class otherwise.
/// </summary>
public partial class Program;
