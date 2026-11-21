// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Chronicle.Workflows.for_WorkflowScheduleService.given;

/// <summary>
/// A clock that only moves when told to, and fires the timer created on it when it does.
/// </summary>
/// <param name="now">The time the clock starts at.</param>
public class ManualClock(DateTimeOffset now) : TimeProvider
{
    readonly TaskCompletionSource<ManualTimer> _timer = new(TaskCreationOptions.RunContinuationsAsynchronously);
    DateTimeOffset _now = now;

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => _now;

    /// <inheritdoc/>
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(callback, state);
        _timer.TrySetResult(timer);
        return timer;
    }

    /// <summary>
    /// Moves the clock forward and fires the timer, once it has been created.
    /// </summary>
    /// <param name="by">How far to move the clock.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Tick(TimeSpan by)
    {
        var timer = await _timer.Task.WaitAsync(TimeSpan.FromSeconds(10), System);
        _now += by;
        timer.Elapse();
    }

    /// <summary>
    /// A timer that fires when the clock is ticked.
    /// </summary>
    /// <param name="callback">The callback to call when it fires.</param>
    /// <param name="state">The state to give the callback.</param>
    public sealed class ManualTimer(TimerCallback callback, object? state) : ITimer
    {
        /// <summary>
        /// Fires the timer.
        /// </summary>
        public void Elapse() => callback(state);

        /// <inheritdoc/>
        public bool Change(TimeSpan dueTime, TimeSpan period) => true;

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
