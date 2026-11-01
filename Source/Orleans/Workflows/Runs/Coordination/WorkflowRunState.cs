// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Adapters;
using Cratis.Orleans.Workflows.Contracts;
using Cratis.Orleans.Workflows.Graphs;
using Cratis.Orleans.Workflows.Waits;

namespace Cratis.Orleans.Workflows.Runs.Coordination;

/// <summary>
/// What a run's event stream says - folded by <see cref="WorkflowRunFold"/>, and nothing else.
/// </summary>
/// <param name="run">The <see cref="WorkflowRunId"/>.</param>
public class WorkflowRunState(WorkflowRunId run)
{
    /// <summary>Gets the <see cref="WorkflowRunId"/>.</summary>
    public WorkflowRunId Run { get; } = run;

    /// <summary>Gets or sets the admission, or null before the run was admitted.</summary>
    public WorkflowRunAdmitted? Admission { get; set; }

    /// <summary>Gets the executions the history recorded anything for.</summary>
    public IDictionary<NodeExecutionKey, NodeExecution> Executions { get; private init; } = new Dictionary<NodeExecutionKey, NodeExecution>();

    /// <summary>Gets the ways out of executions, in the order they were recorded.</summary>
    public IList<WorkflowRoute> Routes { get; private init; } = [];

    /// <summary>Gets the fan-outs, keyed by the for-each execution.</summary>
    public IDictionary<NodeExecutionKey, WorkflowFanOut> FanOuts { get; private init; } = new Dictionary<NodeExecutionKey, WorkflowFanOut>();

    /// <summary>Gets the branch items, keyed by branch.</summary>
    public IDictionary<WorkflowBranchKey, WorkflowBranchOrigin> Branches { get; private init; } = new Dictionary<WorkflowBranchKey, WorkflowBranchOrigin>();

    /// <summary>Gets the open waits.</summary>
    public IDictionary<WorkflowWaitId, WorkflowOpenWait> OpenWaits { get; private init; } = new Dictionary<WorkflowWaitId, WorkflowOpenWait>();

    /// <summary>Gets the immutable inputs of logical executions.</summary>
    public IDictionary<NodeExecutionKey, WorkflowData> Inputs { get; private init; } = new Dictionary<NodeExecutionKey, WorkflowData>();

    /// <summary>Gets job stops awaiting successful delivery, independent of the run outcome.</summary>
    public ISet<WorkflowJobReference> PendingJobStops { get; private init; } = new HashSet<WorkflowJobReference>();

    /// <summary>Gets the completed operations of staged nodes.</summary>
    public IDictionary<(NodeExecutionKey Execution, WorkflowAttempt Attempt, WorkflowOperationName Operation), WorkflowData> Operations { get; private init; } = new Dictionary<(NodeExecutionKey Execution, WorkflowAttempt Attempt, WorkflowOperationName Operation), WorkflowData>();

    /// <summary>Gets or sets the last wave scheduled.</summary>
    public int LastWave { get; set; }

    /// <summary>Gets or sets the failure nothing handled, in the root branch - or null.</summary>
    public WorkflowFailureReason? UnroutedFailure { get; set; }

    /// <summary>Gets or sets whether the run was cancelled.</summary>
    public bool IsCancelled { get; set; }

    /// <summary>Gets or sets how the run finished, or null while it has not.</summary>
    public WorkflowRunStatus? Outcome { get; set; }

    /// <summary>Gets whether the run was admitted.</summary>
    public bool IsAdmitted => Admission is not null;

    /// <summary>Gets whether the run finished or was cancelled - nothing more is planned for it.</summary>
    public bool IsOver => IsCancelled || Outcome is not null;

    /// <summary>Gets the graph the run executes.</summary>
    public WorkflowGraph Graph => Admission?.Graph ?? new WorkflowGraph([], []);

    /// <summary>
    /// Gets when the next retry or wait deadline falls due, or null when nothing is waiting on time.
    /// </summary>
    public DateTimeOffset? NextDueAt
    {
        get
        {
            var due = Executions.Values.Where(execution => execution.Status == NodeExecutionStatus.RetryPending).Select(execution => execution.NotBefore)
                .Concat(OpenWaits.Values.Where(wait => wait.Deadline != WaitDeadline.None).Select(wait => wait.Deadline.Value))
                .ToList();
            return due.Count == 0 ? null : due.Min();
        }
    }

    /// <summary>
    /// Gets where the run stands.
    /// </summary>
    public WorkflowRunStatus Status =>
        IsCancelled ? WorkflowRunStatus.Cancelled
        : Outcome ?? (Executions.Values.Any(execution => execution.Status is NodeExecutionStatus.Scheduled or NodeExecutionStatus.RetryPending) || !Executions.Values.Any(execution => execution.Status == NodeExecutionStatus.Waiting)
            ? WorkflowRunStatus.Running
            : WorkflowRunStatus.Waiting);

    /// <summary>
    /// Gets the execution for a key, creating its record when the history has none yet.
    /// </summary>
    /// <param name="key">The <see cref="NodeExecutionKey"/>.</param>
    /// <returns>The <see cref="NodeExecution"/>.</returns>
    public NodeExecution ExecutionFor(NodeExecutionKey key)
    {
        if (!Executions.TryGetValue(key, out var execution))
        {
            execution = new NodeExecution(key);
            Executions[key] = execution;
        }

        return execution;
    }

    /// <summary>
    /// Creates an independent copy to plan against.
    /// </summary>
    /// <returns>The copy.</returns>
    public WorkflowRunState Clone() => new(Run)
    {
        Admission = Admission,
        Executions = Executions.ToDictionary(pair => pair.Key, pair => pair.Value.Clone()),
        Routes = [.. Routes],
        FanOuts = new Dictionary<NodeExecutionKey, WorkflowFanOut>(FanOuts),
        Branches = new Dictionary<WorkflowBranchKey, WorkflowBranchOrigin>(Branches),
        OpenWaits = new Dictionary<WorkflowWaitId, WorkflowOpenWait>(OpenWaits),
        Operations = new Dictionary<(NodeExecutionKey Execution, WorkflowAttempt Attempt, WorkflowOperationName Operation), WorkflowData>(Operations),
        Inputs = new Dictionary<NodeExecutionKey, WorkflowData>(Inputs),
        PendingJobStops = new HashSet<WorkflowJobReference>(PendingJobStops),
        LastWave = LastWave,
        UnroutedFailure = UnroutedFailure,
        IsCancelled = IsCancelled,
        Outcome = Outcome
    };
}
