// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Tenancy;
using Microsoft.Extensions.Options;

namespace Cratis.Orleans.Workflows.Scopes.for_DefaultWorkflowExecutionScopes;

public class when_mapping_a_scope : Specification
{
    DefaultWorkflowExecutionScopes _scopes = null!;
    DefaultWorkflowExecutionScopes _withTenancy = null!;
    ITenancy _tenancy = null!;
    IDisposable _established = null!;

    void Establish()
    {
        _tenancy = Substitute.For<ITenancy>();
        _tenancy.TenantFor("workflows", "ns-1").Returns("tenant-1");
        _tenancy.Establish("tenant-1").Returns(Substitute.For<IDisposable>());
        _scopes = new(Options.Create(new WorkflowsOptions()));
        _withTenancy = new(Options.Create(new WorkflowsOptions()), _tenancy);
    }

    void Because() => _established = _withTenancy.Enter(new WorkflowScope("tenant-1"));

    [Fact] void should_keep_its_wave_jobs_under_the_jobs_scope_and_the_scope() => _scopes.JobsManagerFor(new WorkflowScope("tenant-1")).ShouldEqual(new JobsManagerKey("workflows", "tenant-1"));
    [Fact] void should_map_a_jobs_manager_key_back_to_the_scope_without_tenancy() => _scopes.ScopeOf(new JobsManagerKey("workflows", "tenant-1")).ShouldEqual(new WorkflowScope("tenant-1"));
    [Fact] void should_ask_the_tenancy_which_tenant_a_jobs_manager_key_is() => _withTenancy.ScopeOf(new JobsManagerKey("workflows", "ns-1")).ShouldEqual(new WorkflowScope("tenant-1"));
    [Fact] void should_establish_the_scope_as_the_tenant() => _tenancy.Received(1).Establish("tenant-1");
    [Fact] void should_establish_nothing_without_tenancy() => _scopes.Enter(new WorkflowScope("tenant-1")).ShouldNotBeNull();
    [Fact] void should_hand_back_what_the_tenancy_established() => _established.ShouldNotBeNull();
}
