// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Serialization;

namespace Cratis.Choreography.Chronicle.Workflows.for_WorkflowRunFacts;

public class when_serializing_every_fact : Specification
{
    Dictionary<string, string> _written = [];

    void Because()
    {
        var serializer = new EventSerializer(
            Substitute.For<IClientArtifactsProvider>(),
            Substitute.For<IClientArtifactsActivator>(),
            Substitute.For<IEventTypes>(),
            Cratis.Json.Globals.JsonSerializerOptions,
            Substitute.For<IDerivedTypes>());
        _written = WorkflowFactSamples.All.ToDictionary(fact => fact.GetType().Name, fact => serializer.Serialize(fact).GetAwaiter().GetResult().ToJsonString());
    }

    [Fact] void should_cover_every_fact_a_run_records() => WorkflowFactSamples.All.Select(fact => fact.GetType().Name).Order().ShouldEqual([.. Cratis.Choreography.Workflows.Runs.WorkflowRunFacts.Types.Select(type => type.Name).Order()]);
    [Fact] void should_have_a_pinned_shape_for_every_fact() => _written.Keys.Order().ShouldEqual([.. WorkflowFactJson.Expected.Keys.Order()]);
    [Fact] void should_write_every_fact_in_the_pinned_shape() => _written.Where(pair => WorkflowFactJson.Expected[pair.Key] != pair.Value).ShouldBeEmpty();
}
