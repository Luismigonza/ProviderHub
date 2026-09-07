using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ProviderHub.Api.IntegrationTests;

[Collection(ApiTests.Name)]
public class SummaryEndpointTests(ApiFactory factory)
{
    private static readonly string[] ColombiaAndPeru = ["CO", "PE"];

    [RequiresDatabaseFact]
    public async Task The_summary_reports_both_indicators_per_country()
    {
        using var client = await factory.CreateSignedInClientAsync();

        var service = await PostAsync(client, "/api/services", new
        {
            name = $"Bandwidth reclamation {Guid.NewGuid():N}"[..40],
            hourlyRate = 64.00m,
        });

        var provider = await PostAsync(client, "/api/providers", new
        {
            nit = "805019876-9",
            name = "Valle Digital Partners",
            website = "https://valledigital.com",
            email = "hello@valledigital.com",
        });

        await PostAsync(
            client,
            $"/api/providers/{provider.GetProperty("id").GetInt32()}/services",
            new { serviceId = service.GetProperty("id").GetInt32(), countries = ColombiaAndPeru });

        var summary = await client.GetFromJsonAsync<JsonElement>(new Uri("/api/summary", UriKind.Relative));

        var colombia = summary.GetProperty("byCountry")
            .EnumerateArray()
            .Single(country => country.GetProperty("countryCode").GetString() == "CO");

        Assert.Equal("Colombia", colombia.GetProperty("countryName").GetString());
        Assert.True(colombia.GetProperty("serviceCount").GetInt32() >= 1);
        Assert.True(colombia.GetProperty("providerCount").GetInt32() >= 1);

        var totals = summary.GetProperty("totals");
        Assert.True(totals.GetProperty("providerCount").GetInt32() >= 1);
        Assert.True(totals.GetProperty("serviceCount").GetInt32() >= 1);
        Assert.True(totals.GetProperty("offeringCount").GetInt32() >= 1);
    }

    [RequiresDatabaseFact]
    public async Task The_summary_needs_a_token_like_everything_else()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/summary", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<JsonElement> PostAsync(HttpClient client, string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
