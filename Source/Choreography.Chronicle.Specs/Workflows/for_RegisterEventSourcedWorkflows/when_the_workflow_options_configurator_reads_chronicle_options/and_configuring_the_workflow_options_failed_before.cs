// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Chronicle.Workflows.for_RegisterEventSourcedWorkflows.given;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Chronicle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Chronicle.Workflows.for_RegisterEventSourcedWorkflows.when_the_workflow_options_configurator_reads_chronicle_options;

public class and_configuring_the_workflow_options_failed_before : a_service_provider_with_a_configurator_reading_chronicle_options
{
    ServiceProvider _failing = null!;
    Exception? _failure;
    ChronicleOptions _chronicle = null!;

    void Establish() => _failing = BuildProvider(configuratorFailures: 1);

    void Because()
    {
        _failure = Catch.Exception(() => _ = _failing.GetRequiredService<IOptions<ChronicleOptions>>().Value);
        _chronicle = _provider.GetRequiredService<IOptions<ChronicleOptions>>().Value;
    }

    void Destroy() => _failing.Dispose();

    [Fact] void should_surface_the_failure() => _failure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_still_register_the_run_facts_on_chronicle_options_built_afterwards_on_the_same_thread() => RegisteredEventTypes.Of(_chronicle.ExplicitArtifacts).Order().ShouldEqual([.. WorkflowRunFacts.Types.Select(type => type.Name).Order()]);
}
