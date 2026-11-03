// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.Jobs;
using Cratis.Choreography.Storage.Sql.Jobs;

using SqlJob = Cratis.Choreography.Storage.Sql.Jobs.Job;

namespace Cratis.Choreography.Storage.Sql.for_JobConverters.when_round_tripping;

public class with_a_finalized_stop : for_JobStorage.given.jobs_in_storage
{
    SqlJob _entity;
    JobState _result;

    void Establish() => _firstJob.StopFinalized = true;

    void Because()
    {
        _entity = ToEntity(_firstJob);
        _result = _entity.ToJobState(_serializerOptions);
    }

    [Fact] void should_write_the_finalized_stop() => _entity.StateJson.ShouldContain("\"stopFinalized\":true");
    [Fact] void should_read_the_finalized_stop() => _result.StopFinalized.ShouldBeTrue();
}
