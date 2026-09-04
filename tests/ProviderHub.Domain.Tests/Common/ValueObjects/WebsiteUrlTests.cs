using ProviderHub.Domain.Common;
using ProviderHub.Domain.Common.ValueObjects;

namespace ProviderHub.Domain.Tests.Common.ValueObjects;

public class WebsiteUrlTests
{
    [Theory]
    [InlineData("https://tekus.co")]
    [InlineData("http://tekus.co/services?page=1")]
    public void An_absolute_http_url_is_accepted(string value)
    {
        var url = WebsiteUrl.Create(value);

        Assert.StartsWith(new Uri(value).Scheme, url.Value, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("tekus.co")]
    [InlineData("/services")]
    public void A_value_that_is_not_an_absolute_url_is_rejected(string? value)
    {
        Assert.Throws<DomainException>(() => WebsiteUrl.Create(value));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///C:/passwords.txt")]
    [InlineData("ftp://tekus.co")]
    public void A_scheme_other_than_http_or_https_is_rejected(string value)
    {
        // Rendering one of these as a link in the frontend would be a security hole,
        // so they never make it past the domain.
        Assert.Throws<DomainException>(() => WebsiteUrl.Create(value));
    }
}
