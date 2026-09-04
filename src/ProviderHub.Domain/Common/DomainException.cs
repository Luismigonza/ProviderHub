namespace ProviderHub.Domain.Common;

/// <summary>
/// Raised when an operation would leave the model in a state the business considers impossible,
/// such as a provider offering the same service twice.
/// <para>
/// This is the model defending its own invariants, the last line of defence rather than the
/// first: user input is validated earlier, in the application layer, so that callers get a
/// friendly <c>400</c> listing every problem instead of an exception about the first one.
/// Reaching this exception through the API means a validation rule is missing upstream.
/// </para>
/// </summary>
public class DomainException : Exception
{
    public DomainException()
    {
    }

    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
