// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Choreography.Storage.Jobs;

namespace Cratis.Choreography.Jobs.for_JobStateConverter;

public class when_round_tripping_a_stored_plan_incarnation : Specification
{
    Guid _incarnation = Guid.NewGuid();
    JobState _result;
    JsonSerializerOptions _options;

    void Establish()
    {
        _options = new();
        _options.Converters.Add(new JobStateConverter(Substitute.For<IJobTypes>()));
    }

    void Because()
    {
        var state = new JobState { Type = new("job"), StoredStepPlanIncarnation = _incarnation };
        _result = JsonSerializer.Deserialize<JobState>(JsonSerializer.Serialize(state, _options), _options)!;
    }

    [Fact] void should_preserve_the_cleanup_and_load_fence() => _result.StoredStepPlanIncarnation.ShouldEqual(_incarnation);
    [Fact]
    void should_allow_older_documents_without_the_fence()
    {
        var document = JsonSerializer.SerializeToNode(new JobState { Type = new("job"), StoredStepPlanIncarnation = _incarnation }, _options)!.AsObject();
        document.Remove("storedStepPlanIncarnation");
        document.Deserialize<JobState>(_options)!.StoredStepPlanIncarnation.ShouldBeNull();
    }
}
