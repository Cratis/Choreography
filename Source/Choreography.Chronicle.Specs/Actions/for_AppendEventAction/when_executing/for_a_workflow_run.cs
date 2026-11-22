// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Actions;
using Cratis.Choreography.Chronicle.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Chronicle.Actions.for_AppendEventAction.when_executing;

public class for_a_workflow_run : given.an_append_event_action
{
    ActionResult _result;

    void Establish()
    {
        AppendSucceeds();
        _services = new ServiceCollection()
            .AddSingleton(_eventStores)
            .AddSingleton(Options.Create(new EventSourcedWorkflowsOptions { EventStore = "workflows-store", NamespaceFor = scope => new($"ns-{scope.Value}") }))
            .BuildServiceProvider();
    }

    async Task Because() => _result = await new AppendEventAction(_ => new SomethingHappened("it")).Execute(
        new(new(GrainKind.WorkflowRun, "cratis-orleans-workflow-run", new Cratis.Choreography.Workflows.WorkflowRunKey(new("tenant-1"), new("run-1")).ToString()), _services),
        CancellationToken.None);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_use_the_event_store_of_the_workflows() => _requestedStore.Value.ShouldEqual("workflows-store");
    [Fact] void should_use_the_namespace_of_the_scope_of_the_run() => _requestedNamespace.Value.ShouldEqual("ns-tenant-1");
    [Fact] void should_append_to_the_run() => _appendedTo.Value.ShouldEqual("run-1");
}
