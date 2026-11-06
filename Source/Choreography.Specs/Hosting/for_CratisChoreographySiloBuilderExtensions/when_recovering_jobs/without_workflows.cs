// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Workflows.Scopes;
using Cratis.Choreography.Workflows.Sweeping;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Choreography.Hosting.for_CratisChoreographySiloBuilderExtensions.when_recovering_jobs;

public class without_workflows : given.a_host_with_jobs_recovery
{
    async Task Because() => await RecoverJobs(workflows: false);

    [Fact] void should_only_include_the_seen_and_host_catalogs() => _providers.Length.ShouldEqual(2);
    [Fact] void should_include_the_seen_catalog() => _providers.OfType<JobsManagerKeys>().Count().ShouldEqual(1);
    [Fact] void should_include_the_host_catalog() => _providers.ShouldContain(_hostKeys);
    [Fact] void should_not_activate_the_workflow_catalog() => _workflowCatalogActivations.ShouldEqual(0);
    [Fact] void should_not_register_workflow_execution_scopes() => _application.Services.GetRequiredService<IServiceProviderIsService>().IsService(typeof(IWorkflowExecutionScopes)).ShouldBeFalse();
    [Fact] void should_not_register_workflow_scopes() => _application.Services.GetRequiredService<IServiceProviderIsService>().IsService(typeof(WorkflowScopes)).ShouldBeFalse();
    [Fact] void should_rehydrate_the_ordinary_manager_once() => _ordinaryManager.Received(1).Rehydrate();
    [Fact] void should_not_rehydrate_a_workflow_manager() => _workflowManager.DidNotReceive().Rehydrate();
}
