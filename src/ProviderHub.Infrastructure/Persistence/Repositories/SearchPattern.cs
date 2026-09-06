namespace ProviderHub.Infrastructure.Persistence.Repositories;

/// <summary>
/// Prepares user input for a <c>LIKE</c> comparison.
/// </summary>
internal static class SearchPattern
{
    /// <summary>
    /// Neutralizes the wildcards of <c>LIKE</c> so that a search term is matched literally.
    /// <para>
    /// Parameters already keep SQL injection out, but they do not stop a term from being read as
    /// a pattern: searching for <c>%</c> would otherwise return every row, and <c>_</c> would
    /// match any character. The brackets turn each wildcard back into an ordinary character.
    /// </para>
    /// </summary>
    public static string Escape(string term) =>
        term
            .Trim()
            .Replace("[", "[[]", StringComparison.Ordinal)
            .Replace("%", "[%]", StringComparison.Ordinal)
            .Replace("_", "[_]", StringComparison.Ordinal);
}
