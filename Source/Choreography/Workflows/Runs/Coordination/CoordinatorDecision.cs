// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Execution;

namespace Cratis.Choreography.Workflows.Runs.Coordination;

/// <summary>
/// What the coordinator decided - the facts to record, the work to schedule and the jobs to stop.
/// </summary>
/// <param name="Events">The events to append to the run's stream, in order.</param>
/// <param name="ToSchedule">The <see cref="WorkflowWaveItem">items</see> to run in a new wave job.</param>
/// <param name="ToStop">The <see cref="WorkflowJobReference">wave jobs</see> to stop.</param>
/// <param name="Wave">The wave the items belong to - zero when there are none.</param>
/// <param name="State">The run as it stands once the events are recorded.</param>
public record CoordinatorDecision(
    IEnumerable<object> Events,
    IEnumerable<WorkflowWaveItem> ToSchedule,
    IEnumerable<WorkflowJobReference> ToStop,
    int Wave,
    WorkflowRunState State)
{
    /// <summary>
    /// Gets whether nothing was decided.
    /// </summary>
    public bool IsEmpty => !Events.Any() && !ToStop.Any();

    /// <summary>
    /// Gets the wave job the items run in.
    /// </summary>
    /// <param name="run">The <see cref="WorkflowRunId"/>.</param>
    /// <param name="wave">The wave.</param>
    /// <returns>The <see cref="WorkflowJobReference"/>.</returns>
    public static WorkflowJobReference JobFor(WorkflowRunId run, int wave) =>
        new(DeterministicId.From(run.Value, "wave", wave.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToString());
}
