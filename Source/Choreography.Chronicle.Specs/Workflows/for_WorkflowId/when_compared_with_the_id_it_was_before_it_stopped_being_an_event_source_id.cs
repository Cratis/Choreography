// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Chronicle;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Serialization;

namespace Cratis.Choreography.Chronicle.Workflows.for_WorkflowId;

/// <summary>
/// The workflow id used to derive from Chronicle's event source id. The core package cannot, but what Chronicle writes and
/// registers for a fact that carries one must not change - the stores that already hold the facts depend on it.
/// </summary>
public class when_compared_with_the_id_it_was_before_it_stopped_being_an_event_source_id : Specification
{
    string _previouslyWritten = null!;
    string _nowWritten = null!;
    string _previousSchema = null!;
    string _currentSchema = null!;
    LegacyFact? _readBack;

    void Because()
    {
        var serializer = new EventSerializer(
            Substitute.For<IClientArtifactsProvider>(),
            Substitute.For<IClientArtifactsActivator>(),
            Substitute.For<IEventTypes>(),
            Cratis.Json.Globals.JsonSerializerOptions,
            Substitute.For<IDerivedTypes>());
        _previouslyWritten = serializer.Serialize(new LegacyFact(new LegacyWorkflowId("workflow-1"))).GetAwaiter().GetResult().ToJsonString();
        _nowWritten = serializer.Serialize(new CurrentFact(new WorkflowId("workflow-1"))).GetAwaiter().GetResult().ToJsonString();
        _readBack = System.Text.Json.JsonSerializer.Deserialize<LegacyFact>(_nowWritten, Cratis.Json.Globals.JsonSerializerOptions);

        var generator = new JsonSchemaGenerator(Substitute.For<IComplianceMetadataResolver>(), new CamelCaseNamingPolicy(), new DerivedTypes(new Cratis.Types.Types()));
        _previousSchema = Normalize(generator.Generate(typeof(LegacyFact)).ToJson());
        _currentSchema = Normalize(generator.Generate(typeof(CurrentFact)).ToJson());
    }

    [Fact] void should_write_the_same_json() => _nowWritten.ShouldEqual(_previouslyWritten);
    [Fact] void should_write_the_id_as_a_plain_string() => _nowWritten.ShouldEqual("{\"workflow\":\"workflow-1\"}");
    [Fact] void should_read_what_it_writes_as_the_previous_id() => _readBack!.Workflow.Value.ShouldEqual("workflow-1");
    [Fact] void should_describe_the_property_with_the_same_schema() => _currentSchema.ShouldEqual(_previousSchema);

    static string Normalize(string schema) => schema.Replace("LegacyFact", "Fact", StringComparison.Ordinal).Replace("CurrentFact", "Fact", StringComparison.Ordinal);

    sealed record LegacyWorkflowId(string Value) : EventSourceId<string>(Value);

    sealed record LegacyFact(LegacyWorkflowId Workflow);

    sealed record CurrentFact(WorkflowId Workflow);
}
