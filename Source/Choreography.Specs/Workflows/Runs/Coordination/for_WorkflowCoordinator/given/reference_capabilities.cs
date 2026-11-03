// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator.given;

/// <summary>Injected adapters used by the reference-workflow proofs, never by production.</summary>
public class reference_capabilities : a_coordinator
{
    protected Dictionary<CapabilityReference, IWorkflowNodePerformer> Performers = [];

    protected void Register(string node, Func<WorkflowNodeInvocation, WorkflowNodeOutcome> perform) =>
        Performers[new($"spec.{node}", 1)] = new fake(new($"spec.{node}", 1), perform);

    protected WorkflowNodeInvocation Invocation(NodeExecutionKey execution) => new(
        Run, execution, AttemptOf(execution), WorkflowPlanner.IdempotencyKeyFor(Run.Run, execution), State.Graph.NodeFor(execution.Node)!.Configuration, NodeInputComposer.For(State, execution), []);

    protected async Task Drive(NodeExecutionKey execution)
    {
        var performer = Performers[State.Graph.NodeFor(execution.Node)!.Capability];
        Report(execution, await performer.Perform(Invocation(execution), CancellationToken.None));
    }

    protected async Task DriveStaged(NodeExecutionKey execution, IStagedWorkflowNodePerformer performer)
    {
        var invocation = Invocation(execution);
        var operations = performer.Operations.ToList();
        var previous = WorkflowData.Empty;
        for (var index = 0; index < operations.Count; index++)
        {
            var outcome = await performer.PerformOperation(invocation, operations[index], previous, CancellationToken.None);
            if (index == operations.Count - 1 || outcome.Status != WorkflowNodeOutcomeStatus.Succeeded)
            {
                Report(execution, outcome);
                return;
            }

            Apply(WorkflowCoordinator.CompleteOperation(Run, State, execution, invocation.Attempt, operations[index], outcome.Output, Now));
            previous = outcome.Output;
        }
    }

    sealed class fake(CapabilityReference capability, Func<WorkflowNodeInvocation, WorkflowNodeOutcome> perform) : IWorkflowNodePerformer
    {
        public CapabilityReference Capability => capability;
        public Task<WorkflowNodeOutcome> Perform(WorkflowNodeInvocation invocation, CancellationToken cancellationToken) => Task.FromResult(perform(invocation));
    }

    protected sealed class staged_publish : IStagedWorkflowNodePerformer
    {
        public Dictionary<WorkflowBranchKey, int> Calls { get; } = [];
        public CapabilityReference Capability => new("spec.release", 1);
        public IEnumerable<WorkflowOperationName> Operations => [new("register"), new("start")];
        public Task<WorkflowNodeOutcome> Perform(WorkflowNodeInvocation invocation, CancellationToken cancellationToken) => throw new InvalidOperationException("Use the staged operations");
        public Task<WorkflowNodeOutcome> PerformOperation(WorkflowNodeInvocation invocation, WorkflowOperationName operation, WorkflowData previous, CancellationToken cancellationToken)
        {
            Calls[invocation.Execution.Branch] = Calls.GetValueOrDefault(invocation.Execution.Branch) + 1;
            if (operation.Value == "register")
            {
                return Task.FromResult(WorkflowNodeOutcome.Succeeded("next", "{\"registered\":true}"));
            }

            var outcome = invocation.Execution.Branch.Value.EndsWith(":second", StringComparison.Ordinal)
                ? WorkflowNodeOutcome.Failed("second target could not start")
                : WorkflowNodeOutcome.Succeeded("next", "{\"started\":true}");
            return Task.FromResult(outcome);
        }
    }
}
