namespace ProviderHub.Domain.Common;

/// <summary>
/// Small helpers for the checks that every entity repeats. They keep the guard clauses at the
/// top of a constructor readable instead of burying the interesting logic under validation.
/// </summary>
public static class DomainGuard
{
    /// <summary>
    /// Ensures a piece of required text is present and within bounds, and returns it trimmed.
    /// </summary>
    /// <exception cref="DomainException">The text is missing, blank or too long.</exception>
    public static string RequiredText(string? value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        var trimmed = value.Trim();

        return trimmed.Length > maxLength
            ? throw new DomainException($"{fieldName} cannot exceed {maxLength} characters.")
            : trimmed;
    }
}
