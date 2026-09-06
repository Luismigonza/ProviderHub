using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ProviderHub.Api.Errors;

/// <summary>
/// Declares the bearer scheme in the OpenAPI document.
/// <para>
/// Without it the generated document says nothing about authentication, and the documentation
/// page has no way to offer an "Authorize" box: every call from it would come back as a 401 with
/// no hint as to why. The scheme is described once here rather than annotated on each endpoint.
/// </para>
/// </summary>
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste the token returned by POST /api/auth/login.",
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>(StringComparer.Ordinal);
        document.Components.SecuritySchemes["Bearer"] = scheme;

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
        });

        return Task.CompletedTask;
    }
}
