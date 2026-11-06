// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Workflows.Sweeping;

namespace Cratis.Choreography.Hosting.for_CratisChoreographySiloBuilderExtensions.when_recovering_jobs;

public class with_workflows : given.a_host_with_jobs_recovery
{
    IEnumerable<JobsManagerKey> _workflowKeys;

    async Task Because()
    {
        await RecoverJobs(workflows: true);
        _workflowKeys = await _providers.OfType<WorkflowJobsManagerKeys>().Single().GetKeys(CancellationToken.None);
    }

    [Fact] void should_include_all_three_catalogs() => _providers.Length.ShouldEqual(3);
    [Fact] void should_include_the_seen_catalog() => _providers.OfType<JobsManagerKeys>().Count().ShouldEqual(1);
    [Fact] void should_include_the_host_catalog() => _providers.ShouldContain(_hostKeys);
    [Fact] void should_activate_the_actual_workflow_catalog_once() => _workflowCatalogActivations.ShouldEqual(1);
    [Fact] void should_map_the_configured_scope_to_the_workflow_manager() => _workflowKeys.Single().ShouldEqual(WorkflowKey);
    [Fact] void should_rehydrate_the_workflow_manager_once() => _workflowManager.Received(1).Rehydrate();
    [Fact] void should_rehydrate_the_ordinary_manager_once() => _ordinaryManager.Received(1).Rehydrate();
}
