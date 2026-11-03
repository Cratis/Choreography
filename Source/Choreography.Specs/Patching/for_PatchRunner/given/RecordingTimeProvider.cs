// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchRunner.given;

/// <summary>
/// A <see cref="TimeProvider"/> whose timers fire immediately and record how long they were asked to wait.
/// </summary>
public class RecordingTimeProvider : TimeProvider
{
    readonly DateTimeOffset _start = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    public List<TimeSpan> Delays { get; } = [];

    /// <summary>
    /// The clock advances by every delay it was asked to wait, so a spec can run out a time budget without waiting for it.
    /// </summary>
    /// <returns>The start of the clock plus everything waited so far.</returns>
    public override DateTimeOffset GetUtcNow() => _start + TimeSpan.FromTicks(Delays.Sum(_ => _.Ticks));

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        Delays.Add(dueTime);
        var timer = new ImmediateTimer();
        Task.Run(() => callback(state));
        return timer;
    }

    sealed class ImmediateTimer : ITimer
    {
        public bool Change(TimeSpan dueTime, TimeSpan period) => true;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
