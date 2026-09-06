using System.Globalization;

namespace ProviderHub.Application.Common;

/// <summary>
/// Raised when a use case is asked to work with something that does not exist.
/// <para>
/// The application layer says "this provider is not here"; it is the API layer that decides
/// this means <c>404</c>. That separation is what lets the same use case run from an HTTP
/// endpoint, a background job or a test without dragging status codes around.
/// </para>
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException()
        : base("The requested resource was not found.")
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Builds the usual "&lt;entity&gt; &lt;key&gt; was not found" message.</summary>
    public static NotFoundException For(string entity, object key) =>
        new(string.Create(CultureInfo.InvariantCulture, $"{entity} '{key}' was not found."));
}

/// <summary>
/// Raised when a sign-in attempt does not match the stored credentials. The API turns it into a
/// <c>401</c>.
/// <para>
/// The message is deliberately vague and identical for every failure: saying which half was
/// wrong turns a login form into a tool for finding out which accounts exist.
/// </para>
/// </summary>
public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("The username or password is incorrect.")
    {
    }

    public InvalidCredentialsException(string message)
        : base(message)
    {
    }

    public InvalidCredentialsException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Raised when a request is well formed but clashes with the current state of the system, such
/// as registering a NIT that already belongs to another provider. The API turns it into a
/// <c>409</c>.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException()
        : base("The request conflicts with the current state of the resource.")
    {
    }

    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
