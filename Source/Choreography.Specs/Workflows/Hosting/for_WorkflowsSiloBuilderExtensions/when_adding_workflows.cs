// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Definitions;
using Cratis.Choreography.Workflows.Journal;
using Cratis.Choreography.Workflows.Scopes;
using Cratis.Choreography.Workflows.Sweeping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Hosting.for_WorkflowsSiloBuilderExtensions;

public class when_adding_workflows : Specification
{
    IServiceCollection _services = null!;
    ServiceProvider _provider = null!;
    WorkflowsOptions _options = null!;

    void Because()
    {
        _services = new ServiceCollection().AddLogging().AddSingleton(Substitute.For<IGrainFactory>()).AddCratisChoreographyWorkflows(options => options.JobsScope = "jobs-for-workflows");
        _provider = _services.BuildServiceProvider();
        _options = _provider.GetRequiredService<IOptions<WorkflowsOptions>>().Value;
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_configure_the_options() => _options.JobsScope.ShouldEqual("jobs-for-workflows");
    [Fact] void should_journal_on_grain_storage() => _provider.GetRequiredService<IWorkflowRunJournal>().ShouldBeOfExactType<GrainStorageWorkflowRunJournal>();
    [Fact] void should_use_the_default_execution_scopes() => _provider.GetRequiredService<IWorkflowExecutionScopes>().ShouldBeOfExactType<DefaultWorkflowExecutionScopes>();
    [Fact] void should_start_workflows_through_the_facade() => _provider.GetRequiredService<IWorkflows>().ShouldBeOfExactType<WorkflowStarter>();
    [Fact] void should_sweep_in_the_background() => _provider.GetServices<IHostedService>().OfType<WorkflowRunSweepService>().Count().ShouldEqual(1);
    [Fact] void should_resolve_performers_per_scope() => _provider.CreateScope().ServiceProvider.GetRequiredService<IWorkflowNodePerformers>().ShouldNotBeNull();
    [Fact] void should_resolve_the_definition_source_per_scope() => _provider.CreateScope().ServiceProvider.GetRequiredService<IWorkflowDefinitionSource>().ShouldNotBeNull();
    [Fact] void should_apply_the_structural_admission_policy() => _provider.GetRequiredService<IEnumerable<IWorkflowAdmissionPolicy>>().OfType<StructuralAdmissionPolicy>().Count().ShouldEqual(1);
}
