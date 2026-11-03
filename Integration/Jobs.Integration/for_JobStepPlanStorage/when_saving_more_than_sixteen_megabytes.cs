// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.for_JobStepPlanStorage;

[Collection(JobsClusterCollection.Name)]
public class when_saving_more_than_sixteen_megabytes : given.a_plan_store
{
    void Establish() => LargePlan();

    async Task Because() => (await _faultingStore.Save(_jobId, _incarnation, _plan)).RethrowError();

    [Fact] void should_split_the_insert_into_batches() => _inserts.ShouldEqual(2);
    [Fact] Task should_round_trip_the_entire_plan_in_order() => AssertStoredPlan();
}
