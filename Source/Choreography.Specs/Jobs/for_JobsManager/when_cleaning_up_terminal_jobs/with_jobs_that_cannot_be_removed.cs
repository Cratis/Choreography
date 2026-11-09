// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.Jobs;

using Catch = Cratis.Monads.Catch;

namespace Cratis.Choreography.Jobs.for_JobsManager.when_cleaning_up_terminal_jobs;

public class with_jobs_that_cannot_be_removed : given.the_manager
{
    protected override JobsOptions CreateOptions() => new()
    {
        TerminalJobRetention = TimeSpan.FromHours(1),
        MaxTerminalJobsPerCleanup = 2
    };

    void Establish()
    {
        _storedJobs.Add(new JobState { Id = Guid.Parse("6f0f1c2e-0000-4000-8000-000000000001"), Status = JobStatus.Failed, Created = DateTimeOffset.UtcNow.AddHours(-4) });
        _storedJobs.Add(new JobState { Id = Guid.Parse("6f0f1c2e-0000-4000-8000-000000000002"), Status = JobStatus.Failed, Created = DateTimeOffset.UtcNow.AddHours(-3) });
        _jobStepStorage.RemoveAllForJob(Arg.Any<JobId>()).Returns(Task.FromResult(Catch.Success()));

        // The default storage substitute reports that nothing was removed, so the same page keeps coming back.
    }

    Task Because() => _manager.CleanupDeadJobs();

    [Fact] void should_stop_when_a_page_holds_nothing_new() => _jobStorage.Received(2).GetJobs(Arg.Is<JobQuery>(query => query.Take == 2 && query.Statuses.Contains(JobStatus.Failed)));
}
