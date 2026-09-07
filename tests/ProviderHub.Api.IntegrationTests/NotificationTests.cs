using System.Net.Http.Json;
using System.Text.Json;

namespace ProviderHub.Api.IntegrationTests;

[Collection(ApiTests.Name)]
public class NotificationTests(ApiFactory factory)
{
    private static readonly string[] ColombiaAndMexico = ["CO", "MX"];

    [RequiresDatabaseFact]
    public async Task Enabling_a_service_sends_the_notification_the_test_asks_for()
    {
        using var client = await factory.CreateSignedInClientAsync();

        var serviceName = $"Packet loss recovery {Guid.NewGuid():N}"[..40];
        var service = await PostAsync(client, "/api/services", new { name = serviceName, hourlyRate = 132.40m });

        var provider = await PostAsync(client, "/api/providers", new
        {
            nit = "860512336-7",
            name = "Pacifico Data Works",
            website = "https://pacificodata.co",
            email = "contact@pacificodata.co",
        });

        var before = CountMessages();

        var response = await client.PostAsJsonAsync(
            $"/api/providers/{provider.GetProperty("id").GetInt32()}/services",
            new { serviceId = service.GetProperty("id").GetInt32(), countries = ColombiaAndMexico });

        response.EnsureSuccessStatusCode();

        // The notification travels the whole way: the aggregate records the event, the unit of
        // work publishes it once the transaction is committed, and the transport writes it out.
        var message = await WaitForNewMessageAsync(before);

        Assert.Contains($"To: {ApiFactory.NotificationRecipient}", message, StringComparison.Ordinal);
        Assert.Contains("Pacifico Data Works has enabled a new service", message, StringComparison.Ordinal);
        Assert.Contains(serviceName, message, StringComparison.Ordinal);
        Assert.Contains("Colombia, Mexico", message, StringComparison.Ordinal);
        Assert.Contains("132.40 USD", message, StringComparison.Ordinal);
    }

    [RequiresDatabaseFact]
    public async Task Adding_a_catalogue_entry_notifies_nobody()
    {
        // The test asks to announce that "a provider has enabled a new service". A catalogue
        // entry that no provider offers yet is not that, so nothing is sent.
        using var client = await factory.CreateSignedInClientAsync();

        var before = CountMessages();

        await PostAsync(client, "/api/services", new
        {
            name = $"Schema archaeology {Guid.NewGuid():N}"[..40],
            hourlyRate = 155.80m,
        });

        await Task.Delay(200);

        Assert.Equal(before, CountMessages());
    }

    private int CountMessages() =>
        Directory.Exists(factory.Outbox) ? Directory.GetFiles(factory.Outbox, "*.eml").Length : 0;

    /// <summary>
    /// The message is written by a handler that runs after the response is produced, so the file
    /// may appear a moment later. Polling briefly beats a fixed sleep long enough to be safe.
    /// </summary>
    private async Task<string> WaitForNewMessageAsync(int before)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (CountMessages() > before)
            {
                var newest = new DirectoryInfo(factory.Outbox)
                    .GetFiles("*.eml")
                    .OrderByDescending(file => file.CreationTimeUtc)
                    .First();

                return await File.ReadAllTextAsync(newest.FullName);
            }

            await Task.Delay(100);
        }

        Assert.Fail("No notification was written to the outbox.");

        return string.Empty;
    }

    private static async Task<JsonElement> PostAsync(HttpClient client, string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
