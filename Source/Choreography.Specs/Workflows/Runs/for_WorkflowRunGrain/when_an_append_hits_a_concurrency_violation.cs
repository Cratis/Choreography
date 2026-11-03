// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs.for_WorkflowRunGrain;

public class when_an_append_hits_a_concurrency_violation : given.a_run
{
    void Establish() => ConflictOnFirstAppend = true;

    Task Because() => Grain.Admit(Definition, Trigger);

    [Fact] void should_read_again() => Reads.ShouldEqual(2);
    [Fact] void should_retry_once() => Appends.ShouldEqual(2);
    [Fact] void should_record_one_admission() => History.Select(item => item.Fact).OfType<WorkflowRunAdmitted>().Count().ShouldEqual(1);
}
