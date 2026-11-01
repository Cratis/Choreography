// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Execution;

namespace Cratis.Orleans.Workflows.Runs.Coordination;

/// <summary>
/// One decision in the making - a working copy of the run that every recorded event is folded into as
/// the coordinator goes, so each step plans against what the previous step already decided.
/// </summary>
/// <param name="run">The <see cref="WorkflowRunKey"/>.</param>
/// <param name="state">The <see cref="WorkflowRunState"/> as recorded - copied, never changed.</param>
/// <param name="now">The time the decision is made at.</param>
public class CoordinatorWork(WorkflowRunKey run, WorkflowRunState state, DateTimeOffset now)
{
    readonly List<object> _events = [];
    readonly List<WorkflowWaveItem> _toSchedule = [];
    readonly List<WorkflowJobReference> _toStop = [];

    /// <summary>Gets the <see cref="WorkflowRunKey"/>.</summary>
    public WorkflowRunKey Run { get; } = run;

    /// <summary>Gets the working <see cref="WorkflowRunState"/>.</summary>
    public WorkflowRunState State { get; } = state.Clone();

    /// <summary>Gets the time the decision is made at.</summary>
    public DateTimeOffset Now { get; } = now;

    /// <summary>
    /// Gets the wave anything scheduled in this decision belongs to.
    /// </summary>
    public int Wave { get; } = state.LastWave + 1;

    /// <summary>
    /// Records an event, folding it into the working state.
    /// </summary>
    /// <param name="event">The event.</param>
    public void Record(object @event)
    {
        _events.Add(@event);
        WorkflowRunFold.Apply(State, @event, Now);
    }

    /// <summary>
    /// Adds an item to run in this decision's wave.
    /// </summary>
    /// <param name="item">The <see cref="WorkflowWaveItem"/>.</param>
    public void Schedule(WorkflowWaveItem item) => _toSchedule.Add(item);

    /// <summary>
    /// Asks for a wave job to be stopped.
    /// </summary>
    /// <param name="job">The <see cref="WorkflowJobReference"/>.</param>
    public void Stop(WorkflowJobReference job) => _toStop.Add(job);

    /// <summary>
    /// Completes the decision.
    /// </summary>
    /// <returns>The <see cref="CoordinatorDecision"/>.</returns>
    public CoordinatorDecision Decide() => new([.. _events], [.. _toSchedule], [.. _toStop.Distinct()], _toSchedule.Count > 0 ? Wave : 0, State);
}
