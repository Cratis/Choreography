// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Orleans.Jobs.Integration.for_JobStepPlanStorage;

[Collection(JobsClusterCollection.Name)]
public class when_a_later_batch_fails : given.a_plan_store
{
    long _committedBeforeFailure;

    void Establish()
    {
        LargePlan();
        _beforeInsert = async attempt =>
        {
            if (attempt == 2)
            {
                _committedBeforeFailure = await _collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
                throw new TimeoutException("Later batch failed");
            }
        };
    }

    async Task Because() => (await _faultingStore.Save(_jobId, _incarnation, _plan)).RethrowError();

    [Fact] void should_have_committed_the_earlier_batch() => (_committedBeforeFailure > 0 && _committedBeforeFailure < _plan.Count).ShouldBeTrue();
    [Fact] void should_delete_the_partial_plan_before_retrying() => _deletes.ShouldEqual(2);
    [Fact] void should_retry_both_batches() => _inserts.ShouldEqual(4);
    [Fact] Task should_store_exactly_one_ordered_copy_of_the_plan() => AssertStoredPlan();
}
