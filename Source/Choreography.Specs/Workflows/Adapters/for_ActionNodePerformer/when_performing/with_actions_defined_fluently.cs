// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Actions;
using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Adapters.for_ActionNodePerformer.when_performing;

public class with_actions_defined_fluently : Specification
{
    static readonly List<string> _log = [];
    WorkflowNodeOutcome _outcome;

    async Task Because()
    {
        var invocation = new WorkflowNodeInvocation(new(new("tenant"), new("run-1")), Runs.NodeExecutionKey.RootOf("node"), new(1), new("idempotency"), WorkflowNodeConfiguration.Empty, new("{}"), []);
        _outcome = await new FluentPerformer(Substitute.For<IServiceProvider>()).Perform(invocation, CancellationToken.None);
    }

    [Fact] void should_succeed() => _outcome.Status.ShouldEqual(WorkflowNodeOutcomeStatus.Succeeded);
    [Fact] void should_execute_the_defined_actions_in_order() => _log.ShouldEqual(["first", "second"]);

    class FluentPerformer(IServiceProvider services) : ActionNodePerformer(services)
    {
        public override CapabilityReference Capability => new("spec.fluent", 1);

        protected override void Define(ActionsBuilder actions) => actions
            .Run(_ => _log.Add("first"))
            .Run(_ => _log.Add("second"));
    }
}
