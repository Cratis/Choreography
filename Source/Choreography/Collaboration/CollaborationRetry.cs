// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Monads;
using Orleans.Runtime.Messaging;

namespace Cratis.Choreography.Collaboration;

/// <summary>
/// Retries a cluster operation that fails transiently while Orleans relocates the activations of a departing
/// silo, within a fixed time budget.
/// </summary>
internal sealed class CollaborationRetry
{
    static readonly int[] _backoffMilliseconds = [250, 500, 1000, 2000, 4000];
    readonly TimeSpan _budget;
    readonly Func<TimeSpan, Task> _delay;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollaborationRetry"/> class with the real clock.
    /// </summary>
    public CollaborationRetry() : this(Task.Delay)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CollaborationRetry"/> class.
    /// </summary>
    /// <param name="delay">How to wait between attempts.</param>
    /// <param name="budget">The total time the operation may take, including the waits; ten seconds when omitted.</param>
    internal CollaborationRetry(Func<TimeSpan, Task> delay, TimeSpan? budget = null)
    {
        _delay = delay;
        _budget = budget ?? TimeSpan.FromSeconds(10);
    }

    /// <summary>
    /// Executes an operation, retrying transient failures until the budget is spent.
    /// </summary>
    /// <typeparam name="T">The type of result the operation produces.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="invalidate">Called after each transient failure, so cached state about the cluster is dropped.</param>
    /// <returns>The result of the operation, or <see cref="CollaborationUnavailable"/> when the budget ran out.</returns>
    public async Task<Result<T, CollaborationUnavailable>> Execute<T>(Func<Task<T>> operation, Action invalidate)
    {
        var stopwatch = Stopwatch.StartNew();
        for (var attempt = 0; attempt <= _backoffMilliseconds.Length; attempt++)
        {
            var remaining = _budget - stopwatch.Elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                break;
            }

            try
            {
                return await operation().WaitAsync(remaining);
            }
            catch (Exception exception) when (IsTransient(exception))
            {
                invalidate();
            }

            if (attempt == _backoffMilliseconds.Length)
            {
                break;
            }

            var delay = TimeSpan.FromMilliseconds(_backoffMilliseconds[attempt]);
            if (stopwatch.Elapsed + delay >= _budget)
            {
                break;
            }

            // Infrastructure readiness backoff while Orleans relocates the departing silo's activations.
            await _delay(delay);
        }

        return new CollaborationUnavailable();
    }

    /// <summary>
    /// Determines whether an exception is the transient kind Orleans reports while it relocates activations.
    /// Orleans reports requests rejected during activation relocation as message rejections. Arbitrary exception
    /// messages are deliberately not matched: programming, serialization and authorization errors must still fail.
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns><see langword="true"/> when the operation is worth retrying; otherwise <see langword="false"/>.</returns>
    internal static bool IsTransient(Exception exception) => exception switch
    {
        SiloUnavailableException or OrleansMessageRejectionException or ConnectionFailedException or TimeoutException => true,
        AggregateException aggregate => aggregate.InnerExceptions.Count > 0 && aggregate.InnerExceptions.All(IsTransient),
        _ => exception.InnerException is not null && IsTransient(exception.InnerException),
    };
}
