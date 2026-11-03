// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.for_JobStepPlanStorage;

[Collection(JobsClusterCollection.Name)]
public class when_an_insert_acknowledgement_is_lost : given.a_plan_store
{
    void Establish()
    {
        LargePlan();
        _afterInsert = attempt => attempt == 2
            ? Task.FromException(new TimeoutException("Insert committed but acknowledgement was lost"))
            : Task.CompletedTask;
    }

    async Task Because() => (await _faultingStore.Save(_jobId, _incarnation, _plan)).RethrowError();

    [Fact] void should_restart_the_save() => _deletes.ShouldEqual(2);
    [Fact] Task should_store_exactly_one_ordered_copy_of_the_plan() => AssertStoredPlan();
}
