// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Choreography.Workflows.Execution;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Json;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Choreography.Workflows.for_WorkflowTransport.when_round_tripping;

public class with_recovery_records : Specification
{
    ServiceProvider _services = null!;
    Serializer _serializer = null!;
    WorkflowWaveRequest _wave = null!;
    WorkflowWaveRequest _copy = null!;
    WorkflowNodeInputPinned _pin = null!;
    WorkflowNodeInputPinned _pinCopy = null!;

    void Establish()
    {
        var json = new JsonSerializerOptions();
        json.Converters.Add(new ConceptAsJsonConverterFactory());
        _services = new ServiceCollection().AddSerializer(builder => builder.AddJsonSerializer(
            type => type.Namespace?.StartsWith("Cratis.Choreography.Workflows", StringComparison.Ordinal) ?? false, json)).BuildServiceProvider();
        _serializer = _services.GetRequiredService<Serializer>();
        var execution = new NodeExecutionKey("publish", "each//2:one", 1);
        _pin = new(execution, "{\"item\":{\"key\":\"one\"}}");
        _wave = new("tenant/run", 2, [new("tenant/run", execution.ToString(), 1, "publish", 1, "{}", _pin.Input.Value, "effect-key", 30, "start", ["evidence"])]);
    }

    void Because()
    {
        _copy = _serializer.Deserialize<WorkflowWaveRequest>(_serializer.SerializeToArray(_wave));
        _pinCopy = _serializer.Deserialize<WorkflowNodeInputPinned>(_serializer.SerializeToArray(_pin));
    }

    void Destroy() => _services.Dispose();

    [Fact] void should_keep_the_wave_identity() => _copy.Run.ShouldEqual(_wave.Run);
    [Fact] void should_keep_the_execution_identity() => NodeExecutionKey.Parse(_copy.Items.Single().Execution).ShouldEqual(_pin.Execution);
    [Fact] void should_keep_the_pinned_input() => _pinCopy.ShouldEqual(_pin);
    [Fact] void should_keep_the_invocation_payload() => _copy.Items.Single().Input.ShouldEqual(_pin.Input.Value);
    [Fact] void should_keep_the_operation_barrier() => _copy.Items.Single().Operation.ShouldEqual("start");
}
