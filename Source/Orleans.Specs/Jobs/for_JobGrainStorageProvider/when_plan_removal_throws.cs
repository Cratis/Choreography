// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobGrainStorageProvider;

public class when_plan_removal_throws : given.a_job_storage_provider
{
    void Establish() => _plans.RemoveForJob(_jobId, _incarnation!.Value).Returns(_ =>
    {
        _removals.Add("plan");
        return Task.FromException<Monads.Catch>(new InvalidOperationException("Removal threw"));
    });

    Task Because() => Clear();

    [Fact] void should_remove_the_job_before_attempting_best_effort_plan_cleanup() => _removals.ShouldEqual(["steps", "job", "plan"]);
}
