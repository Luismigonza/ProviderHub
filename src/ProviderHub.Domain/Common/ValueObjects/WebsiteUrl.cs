namespace ProviderHub.Domain.Common.ValueObjects;

/// <summary>
/// An absolute web address restricted to the <c>http</c> and <c>https</c> schemes.
/// <para>
/// The restriction matters: accepting any absolute URI would let <c>javascript:</c> or
/// <c>file:</c> values reach the frontend, where rendering them as a link is a security hole.
/// Rejecting them here means every layer above can trust the value.
/// </para>
/// </summary>
public sealed record WebsiteUrl
{
    public const int MaxLength = 2048;

    private WebsiteUrl(string value) => Value = value;

    public string Value { get; }

    /// <exception cref="DomainException">The URL is missing, relative or uses an unsupported scheme.</exception>
    public static WebsiteUrl Create(string? value)
    {
        var candidate = DomainGuard.RequiredText(value, MaxLength, "Website");

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
        {
            throw new DomainException($"'{candidate}' is not a valid absolute URL.");
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new DomainException($"'{candidate}' must use the http or https scheme.");
        }

        return new WebsiteUrl(uri.ToString());
    }

    public override string ToString() => Value;
}
