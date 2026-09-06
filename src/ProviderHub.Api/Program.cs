using ProviderHub.Api.Errors;
using ProviderHub.Application;
using ProviderHub.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Failing at startup with a clear message beats failing on the first request with a null
// reference. A misconfigured deployment should never reach the point of accepting traffic.
var connectionString = builder.Configuration.GetConnectionString("ProviderHub")
    ?? throw new InvalidOperationException(
        "Connection string 'ProviderHub' is missing. See appsettings.Development.json for the local one.");

// The composition root, and the only place the three layers are named together.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

builder.Services.AddControllers();

// Every failure leaves as a ProblemDetails, whether it came from an exception or from the
// framework's own model binding.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenApi();

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
app.MapControllers();

await app.RunAsync();

/// <summary>
/// Named so that the integration tests can boot this exact application through
/// <c>WebApplicationFactory</c>. Top-level statements generate an internal class otherwise.
/// </summary>
public partial class Program;
