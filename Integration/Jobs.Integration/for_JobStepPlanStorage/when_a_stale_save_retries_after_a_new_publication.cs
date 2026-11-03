// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Jobs.Integration.for_JobStepPlanStorage;

[Collection(JobsClusterCollection.Name)]
public class when_a_stale_save_retries_after_a_new_publication : given.a_plan_store
{
    Guid _replacement = Guid.NewGuid();

    void Establish()
    {
        LargePlan();
        _afterInsert = async attempt =>
        {
            if (attempt == 1)
            {
                (await _store.Save(_jobId, _replacement, _plan)).RethrowError();
                throw new TimeoutException("Old insert committed but acknowledgement was lost");
            }
        };
    }

    async Task Because() => (await _faultingStore.Save(_jobId, _incarnation, _plan)).RethrowError();

    [Fact] async Task should_retry_without_replacing_the_new_publication() => (await _store.GetForJob(_jobId, _replacement)).AsT0.ShouldEqual(_plan);
    [Fact] async Task should_leave_its_own_publication_complete() => (await _store.GetForJob(_jobId, _incarnation)).AsT0.ShouldEqual(_plan);
}
