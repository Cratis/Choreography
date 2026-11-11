// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Waits;

namespace Cratis.Choreography.Workflows.Runs.Coordination;

/// <summary>
/// Opens, resolves and expires waits - recorded as facts, so nothing holds a worker while a run waits.
/// Running out of time is recorded as the wait resolving through its <c lang="csharp">elapsed</c> or <c lang="csharp">expired</c> outlet - the
/// way the run actually continues - not as a timer firing.
/// </summary>
/// <remarks>
/// A timer is due <c lang="csharp">After</c> its opening and leaves through <c lang="csharp">elapsed</c>. A signal or approval
/// expires <c lang="csharp">ExpiresAfter</c> its opening, or never when that is zero. A signal is resolved through
/// <c lang="csharp">resumed</c> or <c lang="csharp">cancelled</c>, an approval through <c lang="csharp">approved</c> or <c lang="csharp">rejected</c>, and either
/// leaves through <c lang="csharp">expired</c> when it runs out of time. Only an open wait can be resolved or expired:
/// whichever is recorded first wins, and the other is a no-op.
/// </remarks>
public static class WaitHandling
{
    static readonly OutletName[] _signalOutlets = [WellKnownOutlets.Resumed, WellKnownOutlets.Cancelled];
    static readonly OutletName[] _approvalOutlets = [WellKnownOutlets.Approved, WellKnownOutlets.Rejected];

    /// <summary>
    /// Opens the wait of a wait or approval node.
    /// </summary>
    /// <param name="work">The <see cref="CoordinatorWork"/>.</param>
    /// <param name="node">The <see cref="WorkflowNode"/>.</param>
    /// <param name="execution">The <see cref="NodeExecutionKey"/>.</param>
    public static void Open(CoordinatorWork work, WorkflowNode node, NodeExecutionKey execution)
    {
        var configuration = WaitConfiguration.From(node.Configuration) ?? new(WaitKind.Approval, TimeSpan.Zero, TimeSpan.Zero, WorkflowContractReference.Any);
        var kind = node.Kind == WorkflowNodeKind.Approval ? WaitKind.Approval : configuration.Kind;
        var deadline = WaitDeadline.None;
        if (kind == WaitKind.Timer)
        {
            deadline = work.Now + configuration.After;
        }
        else if (configuration.ExpiresAfter > TimeSpan.Zero)
        {
            deadline = work.Now + configuration.ExpiresAfter;
        }

        work.Record(new WorkflowWaitOpened(
            WaitIdFor(work.State.Run, execution),
            execution,
            kind,
            work.State.Admission!.Trigger.Correlation,
            configuration.Signal,
            deadline));
    }

    /// <summary>
    /// Opens the wait of a node execution whose performer accepted the work and answers later.
    /// </summary>
    /// <param name="work">The <see cref="CoordinatorWork"/>.</param>
    /// <param name="execution">The <see cref="NodeExecutionKey"/>.</param>
    /// <param name="key">The <see cref="WorkflowCompletionKey"/> the answer arrives under, recorded as the wait's correlation.</param>
    /// <param name="expiresAfter">How long the wait lasts - zero for never.</param>
    /// <remarks>It expires through <c lang="csharp">expired</c>, like a signal; the node declares the outlet to route it.</remarks>
    public static void OpenForCompletion(CoordinatorWork work, NodeExecutionKey execution, WorkflowCompletionKey key, TimeSpan expiresAfter) =>
        work.Record(new WorkflowWaitOpened(
            WaitIdFor(work.State.Run, execution),
            execution,
            WaitKind.Completion,
            new WorkflowCorrelationKey(key.Value),
            WorkflowContractReference.Any,
            expiresAfter > TimeSpan.Zero ? work.Now + expiresAfter : WaitDeadline.None));

    /// <summary>
    /// Resolves an open wait.
    /// </summary>
    /// <param name="work">The <see cref="CoordinatorWork"/>.</param>
    /// <param name="wait">The <see cref="WorkflowWaitId"/>.</param>
    /// <param name="outlet">The <see cref="OutletName"/> to leave through.</param>
    /// <param name="payload">What came with the resolution.</param>
    /// <param name="actor">The <see cref="WorkflowActor"/> who resolved it.</param>
    /// <returns>True if the wait was open and the outlet is one it can be resolved through.</returns>
    public static bool Resolve(CoordinatorWork work, WorkflowWaitId wait, OutletName outlet, WorkflowData payload, WorkflowActor actor)
    {
        if (!work.State.OpenWaits.TryGetValue(wait, out var open) || !CanBeResolved(work.State, open, outlet, payload))
        {
            return false;
        }

        work.Record(new WorkflowWaitResolved(wait, outlet, payload, actor));
        return true;
    }

    /// <summary>
    /// Expires every open wait whose deadline has passed.
    /// </summary>
    /// <param name="work">The <see cref="CoordinatorWork"/>.</param>
    /// <returns>True if any wait expired.</returns>
    public static bool ExpireDue(CoordinatorWork work)
    {
        var due = work.State.OpenWaits.Values.Where(wait => wait.Deadline.IsDueAt(work.Now)).ToList();
        foreach (var wait in due)
        {
            work.Record(new WorkflowWaitResolved(wait.Wait, wait.Kind == WaitKind.Timer ? WellKnownOutlets.Elapsed : WellKnownOutlets.Expired, WorkflowData.Empty, WorkflowActor.System));
        }

        return due.Count > 0;
    }

    /// <summary>
    /// Cancels every open wait.
    /// </summary>
    /// <param name="work">The <see cref="CoordinatorWork"/>.</param>
    public static void CancelAll(CoordinatorWork work)
    {
        foreach (var wait in work.State.OpenWaits.Keys.ToList())
        {
            work.Record(new WorkflowWaitCancelled(wait));
        }
    }

    /// <summary>
    /// Gets the identity of the wait an execution opens.
    /// </summary>
    /// <param name="run">The <see cref="WorkflowRunId"/>.</param>
    /// <param name="execution">The <see cref="NodeExecutionKey"/>.</param>
    /// <returns>The <see cref="WorkflowWaitId"/>.</returns>
    public static WorkflowWaitId WaitIdFor(WorkflowRunId run, NodeExecutionKey execution) =>
        new(DeterministicId.From(run.Value, execution.ToString()).ToString());

    static bool CanBeResolved(WorkflowRunState state, WorkflowOpenWait open, OutletName outlet, WorkflowData payload) =>
        (open.Kind == WaitKind.Completion ? state.Graph.NodeFor(open.Execution.Node)?.Declares(outlet) == true : CanResolveThrough(open.Kind, outlet))
        && !payload.ExceedsLimit
        && payload.IsValidJson;

    static bool CanResolveThrough(WaitKind kind, OutletName outlet) => kind switch
    {
        WaitKind.Signal => _signalOutlets.Contains(outlet),
        WaitKind.Approval => _approvalOutlets.Contains(outlet),
        _ => false
    };
}
