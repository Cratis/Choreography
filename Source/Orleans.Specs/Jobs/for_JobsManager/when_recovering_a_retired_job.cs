// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobsManager;

/// <summary>
/// The retired activation cannot recover itself, and the job cannot activate while storage is in trouble. The manager keeps
/// asking, with its own activation held alive, until the job takes the resume - and then stops asking and lets go.
/// </summary>
public class when_recovering_a_retired_job : given.the_manager
{
    const int FailingAttempts = 3;

    JobId _jobId;
    Mock<INullJobWithSomeRequest> _job;
    int _attempts;
    TaskCompletionSource _recovered;
    int _attemptsWhenRecovered;

    protected override JobsOptions CreateOptions() => new()
    {
        StepDeliveryRetryDelay = TimeSpan.FromMilliseconds(5),
        MaxStepDeliveryRetryDelay = TimeSpan.FromMilliseconds(20)
    };

    void Establish()
    {
        _jobId = Guid.Parse("24ff9a76-a590-49b7-847d-28fcc9bf1024");
        _recovered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _job = AddJob<INullJobWithSomeRequest>(_jobId);
        _job.Setup(_ => _.Resume()).Returns(() =>
        {
            if (Interlocked.Increment(ref _attempts) <= FailingAttempts)
            {
                return Task.FromException<Result<ResumeJobSuccess, ResumeJobError>>(new InvalidOperationException("Job storage is unavailable"));
            }
            _recovered.TrySetResult();
            return Task.FromResult(Result<ResumeJobSuccess, ResumeJobError>.Success(ResumeJobSuccess.Success));
        });
    }

    async Task Because()
    {
        await _manager.RecoverRetiredJob(_jobId);
        await _manager.RecoverRetiredJob(_jobId);
        await _recovered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _keepAliveReleased.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

        // The release is the last thing the recovery does, so no further attempt can follow it.
        _attemptsWhenRecovered = _attempts;
    }

    [Fact] void should_keep_trying_while_the_job_cannot_activate() => _attemptsWhenRecovered.ShouldEqual(FailingAttempts + 1);
    [Fact] void should_share_one_recovery_between_repeated_requests() => _attempts.ShouldEqual(FailingAttempts + 1);
    [Fact] void should_stop_once_the_job_is_taken_forward() => _attempts.ShouldEqual(_attemptsWhenRecovered);
    [Fact] void should_hold_the_activation_while_it_retries() => _silo.GrainRuntime.Mock.Verify(runtime => runtime.DelayDeactivation(It.IsAny<IGrainContext>(), Timeout.InfiniteTimeSpan), Times.Once);
    [Fact] void should_release_the_activation_when_done() => _silo.GrainRuntime.Mock.Verify(runtime => runtime.DelayDeactivation(It.IsAny<IGrainContext>(), TimeSpan.Zero), Times.Once);
}
