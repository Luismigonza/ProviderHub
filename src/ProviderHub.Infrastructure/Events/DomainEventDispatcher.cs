using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProviderHub.Application.Abstractions.Events;
using ProviderHub.Domain.Common;

namespace ProviderHub.Infrastructure.Events;

/// <summary>
/// Finds the handlers registered for each event and runs them.
/// <para>
/// The lookup is by the event's runtime type, so a new event only needs a handler registered in
/// the container: nothing here changes, and the aggregate that raises it stays unaware that
/// anyone is listening.
/// </para>
/// </summary>
internal sealed partial class DomainEventDispatcher(
    IServiceProvider services,
    ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    /// <summary>
    /// Reflection is unavoidable here, because the event type is only known at runtime. Doing it
    /// once per type rather than once per event keeps it off the hot path.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, HandlerInvocation> Invocations = new();

    public async Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            var invocation = Invocations.GetOrAdd(domainEvent.GetType(), HandlerInvocation.For);

            foreach (var handler in services.GetServices(invocation.HandlerType))
            {
                if (handler is null)
                {
                    continue;
                }

                try
                {
                    await invocation.InvokeAsync(handler, domainEvent, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    // The transaction is already committed by the time this runs. A notification
                    // that cannot be delivered must not undo work that succeeded, nor fail a
                    // request the caller was entitled to have accepted, so the failure is logged
                    // and the remaining handlers still get their turn.
                    LogHandlerFailed(logger, exception, handler.GetType().Name, domainEvent.GetType().Name);
                }
            }
        }
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Handler {Handler} failed while reacting to {DomainEvent}. The change itself was committed.")]
    private static partial void LogHandlerFailed(
        ILogger logger,
        Exception exception,
        string handler,
        string domainEvent);

    /// <summary>The handler interface for one event type, and the method to call on it.</summary>
    private sealed record HandlerInvocation(Type HandlerType, MethodInfo Method)
    {
        public static HandlerInvocation For(Type eventType)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);

            return new HandlerInvocation(
                handlerType,
                handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!);
        }

        public Task InvokeAsync(object handler, IDomainEvent domainEvent, CancellationToken cancellationToken) =>
            (Task)Method.Invoke(handler, [domainEvent, cancellationToken])!;
    }
}
