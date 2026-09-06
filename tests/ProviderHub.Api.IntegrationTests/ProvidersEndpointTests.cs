using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ProviderHub.Api.IntegrationTests;

[Collection(ApiTests.Name)]
public class ProvidersEndpointTests(ApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly string[] ColombiaAndMexico = ["CO", "MX"];
    private static readonly string[] Peru = ["PE"];
    private static readonly string[] LowerAndUpper = ["co", "MX"];

    [RequiresDatabaseFact]
    public async Task A_provider_is_created_and_can_be_read_back_from_its_location_header()
    {
        using var client = await factory.CreateSignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/providers", new
        {
            nit = "890903938-8",
            name = "Importaciones Tekus S.A.",
            website = "https://tekus.co",
            email = "Contact@Tekus.co",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        // Following the Location header rather than guessing the URL: if the header is wrong,
        // this fails, which is the point of sending it.
        var created = await client.GetFromJsonAsync<JsonElement>(response.Headers.Location, Json);

        Assert.Equal("890903938-8", created.GetProperty("nit").GetString());
        Assert.Equal("contact@tekus.co", created.GetProperty("email").GetString());
        Assert.Empty(created.GetProperty("offerings").EnumerateArray());
    }

    [RequiresDatabaseFact]
    public async Task An_unknown_provider_answers_404_as_problem_details()
    {
        using var client = await factory.CreateSignedInClientAsync();

        var response = await client.GetAsync(new Uri("/api/providers/999999", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(404, problem.GetProperty("status").GetInt32());
    }

    [RequiresDatabaseFact]
    public async Task Every_invalid_field_is_reported_in_a_single_response()
    {
        using var client = await factory.CreateSignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/providers", new
        {
            nit = "890903938-1",
            name = "",
            website = "javascript:alert(1)",
            email = "not-an-email",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        var errors = problem.GetProperty("errors");

        // Four mistakes, four field errors, one round trip. A caller does not have to fix them
        // one at a time.
        Assert.Equal(4, errors.EnumerateObject().Count());
        Assert.Contains("check digit", errors.GetProperty("nit")[0].GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("http", errors.GetProperty("website")[0].GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [RequiresDatabaseFact]
    public async Task A_duplicated_tax_identifier_answers_409()
    {
        using var client = await factory.CreateSignedInClientAsync();

        var provider = new
        {
            nit = "811021363-0",
            name = "Bits del Caribe Ltda.",
            website = "https://bitsdelcaribe.com",
            email = "info@bitsdelcaribe.com",
        };

        var first = await client.PostAsJsonAsync("/api/providers", provider);
        var second = await client.PostAsJsonAsync("/api/providers", provider);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [RequiresDatabaseFact]
    public async Task A_service_is_offered_listed_and_withdrawn()
    {
        using var client = await factory.CreateSignedInClientAsync();

        var service = await CreateAsync(client, "/api/services", new
        {
            name = $"Orbital data relay {Guid.NewGuid():N}"[..40],
            hourlyRate = 340.00m,
        });

        var provider = await CreateAsync(client, "/api/providers", new
        {
            nit = "830045781-9",
            name = "Cordillera Software Group",
            website = "https://cordillerasoft.com",
            email = "sales@cordillerasoft.com",
        });

        var providerId = provider.GetProperty("id").GetInt32();
        var serviceId = service.GetProperty("id").GetInt32();

        var offered = await client.PostAsJsonAsync(
            $"/api/providers/{providerId}/services",
            new { serviceId, countries = LowerAndUpper });

        Assert.Equal(HttpStatusCode.Created, offered.StatusCode);

        var details = await offered.Content.ReadFromJsonAsync<JsonElement>(Json);
        var offering = Assert.Single(details.GetProperty("offerings").EnumerateArray().ToList());
        var countries = offering.GetProperty("countries").EnumerateArray().ToList();

        // Normalized on the way in, and resolved to a display name on the way out.
        Assert.Equal(ColombiaAndMexico, countries.Select(country => country.GetProperty("code").GetString()));
        Assert.Equal("Colombia", countries[0].GetProperty("name").GetString());

        var again = await client.PostAsJsonAsync(
            $"/api/providers/{providerId}/services",
            new { serviceId, countries = Peru });

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);

        var withdrawn = await client.DeleteAsync(
            new Uri($"/api/providers/{providerId}/services/{serviceId}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NoContent, withdrawn.StatusCode);

        var afterwards = await client.GetFromJsonAsync<JsonElement>(
            new Uri($"/api/providers/{providerId}", UriKind.Relative), Json);

        Assert.Empty(afterwards.GetProperty("offerings").EnumerateArray());
    }

    private static async Task<JsonElement> CreateAsync(HttpClient client, string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(Json);
    }
}

[Collection(ApiTests.Name)]
public class ListEndpointTests(ApiFactory factory)
{
    private static readonly string[] SortableProviderFields = ["name", "nit", "email"];

    [RequiresDatabaseFact]
    public async Task A_list_answers_with_its_paging_metadata()
    {
        using var client = await factory.CreateSignedInClientAsync();

        var page = await client.GetFromJsonAsync<JsonElement>(
            new Uri("/api/services?page=1&pageSize=5", UriKind.Relative));

        Assert.Equal(1, page.GetProperty("page").GetInt32());
        Assert.Equal(5, page.GetProperty("pageSize").GetInt32());
        Assert.True(page.TryGetProperty("totalCount", out _));
        Assert.True(page.TryGetProperty("hasNextPage", out _));
    }

    [RequiresDatabaseFact]
    public async Task An_oversized_page_is_refused_rather_than_served()
    {
        using var client = await factory.CreateSignedInClientAsync();

        // Anyone can type this in the address bar; without a cap it is a denial of service.
        var response = await client.GetAsync(new Uri("/api/services?pageSize=1000000", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [RequiresDatabaseFact]
    public async Task An_unknown_sort_field_is_refused_rather_than_passed_to_the_database()
    {
        using var client = await factory.CreateSignedInClientAsync();

        var response = await client.GetAsync(
            new Uri("/api/services?sortBy=%3B%20DROP%20TABLE%20Services", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [RequiresDatabaseFact]
    public async Task A_direction_that_is_neither_asc_nor_desc_is_refused()
    {
        using var client = await factory.CreateSignedInClientAsync();

        var response = await client.GetAsync(new Uri("/api/services?direction=sideways", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [RequiresDatabaseFact]
    public async Task The_sortable_fields_of_each_list_are_discoverable()
    {
        using var client = await factory.CreateSignedInClientAsync();

        var fields = await client.GetFromJsonAsync<string[]>(
            new Uri("/api/providers/sort-fields", UriKind.Relative));

        Assert.NotNull(fields);
        Assert.Equal(SortableProviderFields, fields);
    }
}
