// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Actions;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Runs;

namespace Cratis.Choreography.Workflows.Adapters.for_ActionNodePerformer.given;

public abstract class an_action_performer : Specification
{
    protected static readonly WorkflowRunKey Run = new(new("tenant"), new("run-1"));

    protected ActionContext? _contextSeen;
    protected PerformerForTesting _performer;
    protected WorkflowNodeInvocation _invocation;

    void Establish()
    {
        _performer = new(Substitute.For<IServiceProvider>(), (context, _) =>
        {
            _contextSeen = context;
            return Task.FromResult(ActionResultToReturn);
        });

        _invocation = new(Run, NodeExecutionKey.RootOf("node"), new(1), new("idempotency"), WorkflowNodeConfiguration.Empty, new("""{"in":1}"""), []);
    }

    protected abstract ActionResult ActionResultToReturn { get; }

    protected class PerformerForTesting(IServiceProvider services, Func<ActionContext, CancellationToken, Task<ActionResult>> code) : ActionNodePerformer(services)
    {
        public override CapabilityReference Capability => new("spec.action", 1);

        protected override IAction Action { get; } = new CodeAction(code);
    }
}
