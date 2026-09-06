using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ProviderHub.Api.IntegrationTests;

[Collection(ApiTests.Name)]
public class AuthenticationTests(ApiFactory factory)
{
    [Theory]
    [InlineData("/api/providers")]
    [InlineData("/api/services")]
    [InlineData("/api/auth/me")]
    public async Task Every_endpoint_refuses_an_anonymous_caller(string url)
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri(url, UriKind.Relative));

        // Authorization is the default rather than an opt-in, so an endpoint added tomorrow is
        // protected without anyone having to remember.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Signing_in_returns_a_bearer_token_that_opens_the_api()
    {
        using var client = factory.CreateClient();

        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { userName = ApiFactory.UserName, password = ApiFactory.Password });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var token = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Bearer", token.GetProperty("tokenType").GetString());
        Assert.True(token.GetProperty("expiresAt").GetDateTimeOffset() > DateTimeOffset.UtcNow);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.GetProperty("accessToken").GetString());

        var services = await client.GetAsync(new Uri("/api/services", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, services.StatusCode);
    }

    [Theory]
    [InlineData("wrong-user", ApiFactory.Password)]
    [InlineData(ApiFactory.UserName, "wrong-password")]
    public async Task Wrong_credentials_answer_401(string userName, string password)
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName, password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        // The same answer either way: the response must not reveal which half was wrong.
        Assert.Equal(
            "The username or password is incorrect.",
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task The_signed_in_user_is_reported_back()
    {
        using var client = await factory.CreateSignedInClientAsync();

        var me = await client.GetFromJsonAsync<JsonElement>(new Uri("/api/auth/me", UriKind.Relative));

        Assert.Equal(ApiFactory.UserName, me.GetProperty("userName").GetString());
    }

    [Fact]
    public async Task A_token_whose_payload_was_edited_is_refused()
    {
        using var signedIn = await factory.CreateSignedInClientAsync();
        var original = signedIn.DefaultRequestHeaders.Authorization!.Parameter!;

        var parts = original.Split('.');
        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            Base64UrlDecode(parts[1]))!;

        // Claiming to be somebody else, keeping the original signature.
        payload["sub"] = JsonSerializer.SerializeToElement("attacker");

        var forged = string.Join('.', parts[0], Base64UrlEncode(JsonSerializer.Serialize(payload)), parts[2]);

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", forged);

        var response = await client.GetAsync(new Uri("/api/services", UriKind.Relative));

        // The signature covers the payload, and the signature is verified. A token is only as
        // trustworthy as the check that nobody bothered to switch off.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_token_signed_with_another_key_is_refused()
    {
        using var client = factory.CreateClient();

        // A structurally perfect JWT, signed by someone else.
        const string foreignToken =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9" +
            ".eyJzdWIiOiJhZG1pbiIsIm5hbWUiOiJhZG1pbiIsImV4cCI6NDEwMjQ0NDgwMH0" +
            ".Zm9yZ2VkLXNpZ25hdHVyZS10aGF0LWRvZXMtbm90LW1hdGNo";

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", foreignToken);

        var response = await client.GetAsync(new Uri("/api/services", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/').PadRight(
            value.Length + ((4 - (value.Length % 4)) % 4),
            '=');

        return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
    }

    private static string Base64UrlEncode(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
