// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Orleans.Workflows.Adapters;
using Cratis.Orleans.Workflows.Execution;
using Cratis.Orleans.Workflows.Graphs;

namespace Cratis.Orleans.Workflows.Runs.Coordination;

/// <summary>
/// Plans what a run does next from what it has recorded - until nothing more can happen without an
/// outcome, a signal or the passing of time.
/// </summary>
/// <remarks>
/// Every activated execution the history says nothing about yet is taken forward: a trigger completes
/// with the trigger's payload, an action or decision is scheduled into this decision's wave, a wait
/// opens, a for-each fans out and a join gathers once it can. Each step is recorded before the next is
/// planned, so a wave only ever holds executions that were ready from recorded state. When nothing is
/// left in flight, the run finishes.
/// </remarks>
public static class WorkflowPlanner
{
    /// <summary>
    /// Plans a run forward.
    /// </summary>
    /// <param name="work">The <see cref="CoordinatorWork"/> to record into.</param>
    public static void Plan(CoordinatorWork work)
    {
        if (!work.State.IsAdmitted || work.State.IsOver)
        {
            return;
        }

        while (Step(work))
        {
        }

        ReachabilityAnalysis.FinishIfQuiescent(work);
    }

    /// <summary>
    /// Schedules an attempt at an action or decision into the decision's wave.
    /// </summary>
    /// <param name="work">The <see cref="CoordinatorWork"/>.</param>
    /// <param name="execution">The <see cref="NodeExecutionKey"/>.</param>
    /// <param name="attempt">The <see cref="WorkflowAttempt"/>.</param>
    public static void Schedule(CoordinatorWork work, NodeExecutionKey execution, WorkflowAttempt attempt)
    {
        var state = work.State;
        var node = state.Graph.NodeFor(execution.Node)!;
        var input = state.Inputs.TryGetValue(execution, out var pinned) ? pinned : NodeInputComposer.For(state, execution);
        if (input.ExceedsLimit || !input.IsValidJson)
        {
            OutcomeRecording.RejectData(work, execution, attempt, "The composed input is not bounded valid JSON");
            return;
        }

        if (!state.Inputs.ContainsKey(execution))
        {
            work.Record(new WorkflowNodeInputPinned(execution, input));
        }

        var item = new WorkflowWaveItem(
            work.Run.ToString(),
            execution.ToString(),
            attempt.Value,
            node.Capability.Id.Value,
            node.Capability.Version.Value,
            node.Configuration.Value,
            input.Value,
            IdempotencyKeyFor(state.Run, execution).Value,
            (int)Math.Ceiling(node.Policy.AttemptTimeout.TotalSeconds),
            WorkflowOperationName.None.Value,
            [.. state.Admission!.Trigger.Evidence.Select(evidence => evidence.Value)]);

        work.Record(new WorkflowNodeScheduled(execution, attempt, CoordinatorDecision.JobFor(state.Run, work.Wave), work.Wave));
        work.Schedule(item);
    }

    /// <summary>
    /// Gets the key performers deduplicate an execution's external effects on - the same for every attempt.
    /// </summary>
    /// <param name="run">The <see cref="WorkflowRunId"/>.</param>
    /// <param name="execution">The <see cref="NodeExecutionKey"/>.</param>
    /// <returns>The <see cref="WorkflowIdempotencyKey"/>.</returns>
    public static WorkflowIdempotencyKey IdempotencyKeyFor(WorkflowRunId run, NodeExecutionKey execution) =>
        new(DeterministicId.From(run.Value, execution.Node.Value, execution.Branch.Value, execution.Visit.ToString(CultureInfo.InvariantCulture)).ToString());

    static bool Step(CoordinatorWork work)
    {
        var state = work.State;
        var analysis = ActivationAnalysis.Of(state);
        var progressed = false;

        foreach (var execution in analysis.Activations)
        {
            if (state.Executions.ContainsKey(execution) || state.UnroutedFailure is not null || IsInClosedBranch(state, execution))
            {
                continue;
            }

            if (state.Graph.NodeFor(execution.Node) is not { } node)
            {
                continue;
            }

            progressed |= TakeForward(work, analysis, node, execution);
        }

        return progressed;
    }

    static bool TakeForward(CoordinatorWork work, ActivationAnalysis analysis, WorkflowNode node, NodeExecutionKey execution)
    {
        var state = work.State;
        if (execution.Visit > node.Policy.MaxVisits)
        {
            var reason = new WorkflowFailureReason($"'{node.Id}' was reached {execution.Visit} times, more than the {node.Policy.MaxVisits} it allows");
            work.Record(new WorkflowNodeAttemptFailed(execution, WorkflowAttempt.First, WorkflowFailureKind.VisitLimitExceeded, reason));
            work.Record(new WorkflowNodeFailed(execution, WorkflowAttempt.First, WorkflowFailureKind.VisitLimitExceeded, reason, FailureRouting.For(state.Graph, node.Id, WorkflowFailureKind.VisitLimitExceeded)));
            return true;
        }

        switch (node.Kind)
        {
            case WorkflowNodeKind.Trigger:
                var trigger = state.Admission!.Trigger;
                work.Record(new WorkflowNodeSucceeded(execution, WorkflowAttempt.First, WellKnownOutlets.Next, trigger.Payload, [.. trigger.Evidence]));
                return true;

            case WorkflowNodeKind.Action or WorkflowNodeKind.Decision:
                if (!ForEachExpansion.HasCapacityFor(state, execution))
                {
                    return false;
                }

                Schedule(work, execution, WorkflowAttempt.First);
                return true;

            case WorkflowNodeKind.Wait or WorkflowNodeKind.Approval:
                WaitHandling.Open(work, node, execution);
                return true;

            case WorkflowNodeKind.ForEach:
                ForEachExpansion.FanOut(work, node, execution);
                return true;

            case WorkflowNodeKind.Join:
                return JoinEvaluation.TryJoin(work, analysis, node, execution);

            default:
                return false;
        }
    }

    static bool IsInClosedBranch(WorkflowRunState state, NodeExecutionKey execution) =>
        state.Branches.TryGetValue(execution.Branch, out var origin)
            && state.FanOuts.TryGetValue(origin.ForEach, out var fanOut)
            && fanOut.IsJoined;
}
