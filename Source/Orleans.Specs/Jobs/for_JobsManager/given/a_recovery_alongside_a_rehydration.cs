// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobsManager.given;

/// <summary>
/// A rehydration that is draining a blocked backlog, with a retired job's recovery requested while it drains. Both hold the
/// manager's activation, and it is released only when the last of them lets go.
/// </summary>
public class a_recovery_alongside_a_rehydration : a_controlled_backlog
{
    protected TaskCompletionSource<Result<ResumeJobSuccess, ResumeJobError>> _recoveryCompletion;

    protected int Releases => _silo.GrainRuntime.Mock.Invocations.Count(_ => _.Method.Name == nameof(IGrainRuntime.DelayDeactivation) && Equals(_.Arguments[1], TimeSpan.Zero));

    async Task Establish()
    {
        _options.Value.Returns(new JobsOptions
        {
            MaxConcurrentRehydration = 2,
            StepDeliveryRetryDelay = TimeSpan.FromMilliseconds(5),
            MaxStepDeliveryRetryDelay = TimeSpan.FromMilliseconds(20)
        });
        _recoveryCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await _manager.Rehydrate().WaitAsync(Deadline, TimeProvider.System);
        await _started[1].Task.WaitAsync(Deadline, TimeProvider.System);

        // The drain took its snapshot of stored jobs already, so the retired job is only ever seen by its recovery.
        var recovering = AddJob<INullJobWithSomeRequest>(JobId.New());
        recovering.Setup(job => job.Resume()).Returns(() => _recoveryCompletion.Task);
        await _manager.RecoverRetiredJob(_storedJobs[^1].Id);
    }

    protected async Task WaitForARelease()
    {
        var deadline = DateTime.UtcNow.Add(Deadline);
        while (DateTime.UtcNow < deadline && Releases == 0)
        {
            await Task.Delay(5);
        }
    }

    protected void CompleteTheRecovery() => _recoveryCompletion.TrySetResult(Result<ResumeJobSuccess, ResumeJobError>.Success(ResumeJobSuccess.Success));
}
