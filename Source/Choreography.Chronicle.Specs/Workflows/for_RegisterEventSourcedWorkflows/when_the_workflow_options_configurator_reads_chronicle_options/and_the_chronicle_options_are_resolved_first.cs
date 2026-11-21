// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Chronicle.Workflows.for_RegisterEventSourcedWorkflows.given;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Chronicle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Chronicle.Workflows.for_RegisterEventSourcedWorkflows.when_the_workflow_options_configurator_reads_chronicle_options;

public class and_the_chronicle_options_are_resolved_first : a_service_provider_with_a_configurator_reading_chronicle_options
{
    ChronicleOptions _chronicle = null!;
    ChronicleClientOptions _client = null!;
    EventSourcedWorkflowsOptions _workflows = null!;

    void Because()
    {
        _chronicle = _provider.GetRequiredService<IOptions<ChronicleOptions>>().Value;
        _client = _provider.GetRequiredService<IOptions<ChronicleClientOptions>>().Value;
        _workflows = _provider.GetRequiredService<IOptions<EventSourcedWorkflowsOptions>>().Value;
    }

    [Fact] void should_configure_the_workflow_options() => _workflows.EventStore.Value.ShouldEqual(ConfiguratorReadingChronicleOptions.EventStore);
    [Fact] void should_register_the_run_facts_on_the_chronicle_options() => RegisteredEventTypes.Of(_chronicle.ExplicitArtifacts).Order().ShouldEqual([.. WorkflowRunFacts.Types.Select(type => type.Name).Order()]);
    [Fact] void should_register_the_run_facts_on_the_chronicle_client_options() => RegisteredEventTypes.Of(_client.ExplicitArtifacts).Order().ShouldEqual([.. WorkflowRunFacts.Types.Select(type => type.Name).Order()]);
}
