// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStateSerializer.when_round_tripping;

public class with_a_stored_plan_incarnation : for_JobStorage.given.a_job_storage
{
    Guid _incarnation = Guid.NewGuid();
    JobState _result;

    void Establish() => _firstJob.StoredStepPlanIncarnation = _incarnation;

    void Because() => _result = BsonSerializer.Deserialize<JobState>(_firstJob.ToBsonDocument());

    [Fact] void should_preserve_the_cleanup_and_load_fence() => _result.StoredStepPlanIncarnation.ShouldEqual(_incarnation);
    [Fact]
    void should_allow_older_documents_without_the_fence()
    {
        var document = _firstJob.ToBsonDocument();
        document.Remove("storedStepPlanIncarnation");
        BsonSerializer.Deserialize<JobState>(document).StoredStepPlanIncarnation.ShouldBeNull();
    }
}
