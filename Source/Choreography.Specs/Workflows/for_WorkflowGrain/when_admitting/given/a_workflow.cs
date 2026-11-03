// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Definitions;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Choreography.Workflows.Scopes;
using Cratis.Choreography.Workflows.Triggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.TestKit;

namespace Cratis.Choreography.Workflows.for_WorkflowGrain.when_admitting.given;

public class a_workflow : Specification
{
    protected readonly WorkflowKey Key = new(new WorkflowScope("tenant"), WorkflowId.New());
    protected IWorkflowDefinitionSource Source = null!;
    protected IWorkflowAdmissionPolicy Policy = null!;
    protected WorkflowGrain Grain = null!;
    protected WorkflowAdmission Admission = null!;
    protected WorkflowTrigger Trigger = new(WorkflowTriggerKind.Manual, "spec", "dedup", new("spec", "subject"), WorkflowContractReference.Any, WorkflowData.Empty, [], "correlation", WorkflowCausationKey.None, WorkflowActor.System, DateTimeOffset.UtcNow);
    ServiceProvider _provider = null!;

    async Task Establish()
    {
        Source = Substitute.For<IWorkflowDefinitionSource>();
        Policy = Substitute.For<IWorkflowAdmissionPolicy>();
        Policy.Check(Arg.Any<WorkflowKey>(), Arg.Any<WorkflowDefinitionSnapshot>(), Arg.Any<WorkflowTrigger>()).Returns([]);
        var policies = Substitute.For<IInstancesOf<IWorkflowAdmissionPolicy>>();
        policies.GetEnumerator().Returns(_ => new List<IWorkflowAdmissionPolicy> { Policy }.GetEnumerator());
        _provider = new ServiceCollection().AddSingleton(Source).AddSingleton(policies).BuildServiceProvider();
        var silo = new TestKitSilo();
        silo.AddService(_provider.GetRequiredService<IServiceScopeFactory>());
        silo.AddService<IWorkflowExecutionScopes>(new DefaultWorkflowExecutionScopes(Options.Create(new WorkflowsOptions())));
        Grain = await silo.CreateGrainAsync<WorkflowGrain>(Key.ToString());
    }

    protected void Activate() => Source.GetActive(Key).Returns(new WorkflowDefinitionSnapshot(Key.Workflow, "stored-area", 1, "spec", new WorkflowGraph([], []), "catalog", []));

    async ValueTask Destroy() => await _provider.DisposeAsync();
}
