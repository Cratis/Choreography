// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Definitions;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Triggers;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator.given;

/// <summary>
/// A coordinator over a run the spec builds a graph for, recording every decision as the grain would.
/// </summary>
public class a_coordinator : Specification
{
    protected static readonly DateTimeOffset Start = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    protected WorkflowRunKey Run = new(new WorkflowScope("tenant"), WorkflowRunId.For("workflow", Guid.NewGuid().ToString()));
    protected DateTimeOffset Now = Start;
    protected WorkflowRunState State = null!;
    protected CoordinatorDecision Decision = null!;
    protected List<object> History = [];

    protected static WorkflowNode Trigger(string id = "trigger") =>
        new(id, WorkflowNodeKind.Trigger, new("workflows.trigger.manual", 1), WorkflowNodeConfiguration.Empty, [WellKnownOutlets.Next], WorkflowNodePolicy.Default);

    protected static WorkflowNode Action(string id, WorkflowNodePolicy? policy = null, params string[] outlets) =>
        new(id, WorkflowNodeKind.Action, new($"spec.{id}", 1), WorkflowNodeConfiguration.Empty, Outlets(outlets, WellKnownOutlets.Next), policy ?? WorkflowNodePolicy.Default);

    protected static WorkflowNode Decider(string id, params string[] outlets) =>
        new(id, WorkflowNodeKind.Decision, new($"spec.{id}", 1), WorkflowNodeConfiguration.Empty, Outlets(outlets, WellKnownOutlets.Next), WorkflowNodePolicy.Default);

    protected static WorkflowNode ForEach(string id, string itemsFrom, string itemsProperty, int maxParallelism = 10) =>
        new(id, WorkflowNodeKind.ForEach, new("workflows.for-each", 1), $$$"""{"itemsFrom":"{{{itemsFrom}}}","itemsProperty":"{{{itemsProperty}}}","maxParallelism":{{{maxParallelism}}}}""", [WellKnownOutlets.Each, WellKnownOutlets.Failed, WellKnownOutlets.Fallback], WorkflowNodePolicy.Default);

    protected static WorkflowNode Join(string id, string forEach, JoinMode mode = JoinMode.All, int threshold = 0) =>
        new(id, WorkflowNodeKind.Join, new("workflows.join", 1), $$$"""{"forEach":"{{{forEach}}}","mode":"{{{mode.ToString().ToLowerInvariant()}}}","threshold":{{{threshold}}}}""", [WellKnownOutlets.Completed, WellKnownOutlets.Partial], WorkflowNodePolicy.Default);

    protected static WorkflowNode Timer(string id, TimeSpan after) =>
        new(id, WorkflowNodeKind.Wait, new("workflows.wait.timer", 1), $$$"""{"kind":"timer","after":"{{{after:c}}}"}""", [WellKnownOutlets.Elapsed], WorkflowNodePolicy.Default);

    protected static WorkflowNode SignalWait(string id, TimeSpan expiresAfter) =>
        new(id, WorkflowNodeKind.Wait, new("workflows.wait.signal", 1), $$$"""{"kind":"signal","expiresAfter":"{{{expiresAfter:c}}}","signal":{"id":"spec.signal","version":1}}""", [WellKnownOutlets.Resumed, WellKnownOutlets.Expired, WellKnownOutlets.Cancelled], WorkflowNodePolicy.Default);

    protected static WorkflowNode Approval(string id, TimeSpan expiresAfter) =>
        new(id, WorkflowNodeKind.Approval, new("workflows.approval", 1), $$$"""{"kind":"approval","expiresAfter":"{{{expiresAfter:c}}}"}""", [WellKnownOutlets.Approved, WellKnownOutlets.Rejected, WellKnownOutlets.Expired], WorkflowNodePolicy.Default);

    protected static WorkflowEdge Edge(string from, string outlet, string to) => new(from, outlet, to);

    protected static NodeExecutionKey Root(string node, int visit = 1) => new(node, WorkflowBranchKey.Root, visit);

    protected static NodeExecutionKey InBranch(string node, string forEach, string item, int visit = 1) => new(node, $"{Root(forEach)}:{Uri.EscapeDataString(item)}", visit);

    protected IEnumerable<TEvent> Recorded<TEvent>() => Decision.Events.OfType<TEvent>();

    protected IEnumerable<NodeExecutionKey> ScheduledNow => Recorded<WorkflowNodeScheduled>().Select(scheduled => scheduled.Execution);

    protected void Admit(IEnumerable<WorkflowNode> nodes, IEnumerable<WorkflowEdge> edges, string payload = "{}")
    {
        State = new WorkflowRunState(Run.Run);
        var definition = new WorkflowDefinitionSnapshot("workflow", "spec-area", 1, "spec-subject", new WorkflowGraph([.. nodes], [.. edges]), "catalog-1", []);
        Apply(WorkflowCoordinator.Admit(Run, State, definition, TriggerWith(payload), Now));
    }

    protected void Succeed(NodeExecutionKey execution, string outlet = "next", string output = "{}") =>
        Report(execution, WorkflowNodeOutcome.Succeeded(outlet, output));

    protected void Report(NodeExecutionKey execution, WorkflowNodeOutcome outcome) =>
        Apply(WorkflowCoordinator.CompleteNode(Run, State, execution, AttemptOf(execution), outcome, Now));

    protected void Report(NodeExecutionKey execution, WorkflowAttempt attempt, WorkflowNodeOutcome outcome) =>
        Apply(WorkflowCoordinator.CompleteNode(Run, State, execution, attempt, outcome, Now));

    protected void Fail(NodeExecutionKey execution, WorkflowFailureKind kind = WorkflowFailureKind.Error) =>
        Apply(WorkflowCoordinator.FailAttempt(Run, State, execution, AttemptOf(execution), kind, "it went wrong", Now));

    protected void ResolveWaitOf(NodeExecutionKey execution, string outlet, string payload = "{}") =>
        Apply(WorkflowCoordinator.ResolveWait(Run, State, WaitHandling.WaitIdFor(Run.Run, execution), outlet, payload, "someone", Now));

    protected void ExpireWaitsAt(DateTimeOffset at) => Apply(WorkflowCoordinator.ExpireDueWaits(Run, State, at));

    protected void StartRetriesAt(DateTimeOffset at) => Apply(WorkflowCoordinator.StartDueRetries(Run, State, at));

    protected void Cancel() => Apply(WorkflowCoordinator.Cancel(Run, State, "called off", Now));

    protected WorkflowAttempt AttemptOf(NodeExecutionKey execution) => State.Executions[execution].Attempt;

    protected NodeExecutionStatus StatusOf(NodeExecutionKey execution) => State.Executions[execution].Status;

    protected void Apply(CoordinatorDecision decision)
    {
        Decision = decision;
        History.AddRange(decision.Events);
        State = decision.State;
    }

    static WorkflowTrigger TriggerWith(string payload) => new(
        WorkflowTriggerKind.Manual,
        "spec",
        Guid.NewGuid().ToString(),
        new("spec-subject", "subject-1"),
        WorkflowContractReference.Any,
        payload,
        [],
        "correlation-1",
        WorkflowCausationKey.None,
        WorkflowActor.System,
        Start);

    static IEnumerable<OutletName> Outlets(string[] outlets, OutletName fallback) =>
        outlets.Length == 0 ? [fallback] : [.. outlets.Select(outlet => new OutletName(outlet))];
}
