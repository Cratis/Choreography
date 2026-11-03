// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Jobs.for_JobGrainStorageProvider;

public class when_the_job_never_published_a_plan : given.a_job_storage_provider
{
    void Establish() => _incarnation = null;

    Task Because() => Clear();

    [Fact] void should_remove_the_job_and_steps() => _removals.ShouldEqual(["steps", "job"]);
    [Fact] void should_not_contact_plan_storage() => _plans.ReceivedCalls().ShouldBeEmpty();
}
