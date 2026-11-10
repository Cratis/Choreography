// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Choreography.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    /// <summary>
    /// Answers a resume of a prepared, stopped job that has nothing to resume, reloading what is left to run first.
    /// </summary>
    /// <returns>The answer, or null when there are steps to resume.</returns>
    async Task<Result<ResumeJobSuccess, ResumeJobError>?> AnswerResumeFromSteps()
    {
        if (!await CanResume())
        {
            return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.JobCannotBeResumed);
        }

        if (!JobIsPrepared())
        {
            return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.JobIsNotPrepared);
        }

        // Steps in a stage that never started are still tracked after the running ones stopped, so a job with
        // stages always reloads what is left from the steps themselves.
        if (ResumeNeedsStepReload)
        {
            _jobStepGrains = await GetIdAndGrainReferenceForNonCompletedJobSteps();
        }

        // A prepared job with nothing left to start cannot make progress on its own, so resuming it only puts
        // it back into Running for the next resume to find. That is the same permanent wedge as the state
        // above, reached when the progress for a completed step was never persisted (#3944). The steps
        // themselves are the record of what happened, so recount from them before finalizing.
        if (_jobStepGrains is null or { Count: 0 })
        {
            _logger.FinalizingJobLeftRunningAfterAllStepsCompleted();
            await ReconcileProgressFromJobSteps();
            return await FinalizeInsteadOfResuming();
        }

        // A job with stages resumes at the stage it had reached - unless the stage before it failed as a barrier
        // and a stop left that unacted on, in which case there is nothing to resume.
        if (await PrepareStagesForResume())
        {
            return await FinalizeInsteadOfResuming();
        }

        return null;
    }

    /// <summary>
    /// Begins a new execution of the job's steps, undoing the entry when it cannot be made durable or the hook rejects it.
    /// </summary>
    /// <param name="stoppedProgress">The progress to restore if the resume is rejected, when the entry state allowed it.</param>
    /// <returns>The result of resuming the current stage.</returns>
    async Task<Result<ResumeJobSuccess, ResumeJobError>> BeginResumedExecution(JobProgress? stoppedProgress)
    {
        _logger.Resuming();
        var stoppedSteps = State.Progress.StoppedSteps;
        var countedSteps = (Stopped: State.StoppedStepsCounted, Completed: State.CompletedStepsCounted.ToList());

        // The new execution has counted no stops; that is written with it, so no activation can mistake a stop of
        // the new execution for one it already counted.
        ForgetCountedStops();

        // Resuming begins a new execution; its steps carry this change's position, so none may start unless it is durable.
        if (!await TryWriteStatusChanged(JobStatus.Running))
        {
            RestoreCountsAfterFailedResume(stoppedSteps, countedSteps, stoppedProgress);
            return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.Unknown);
        }

        if (!await TryRunResumeHook(stoppedProgress, countedSteps.Stopped, countedSteps.Completed))
        {
            return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.Unknown);
        }

        // Only a successful hook begins dispatch. Until then, late drained reports must stay accounted for.
        // _outcomesCounted is kept, backed by the durable record across activations, so an earlier-counted outcome cannot count twice.
        _accountedByDrain.Clear();
        _started.Clear();
        _inFlight.Clear();
        return await ResumeCurrentStage(StepsInCurrentStage(), reportAlreadyRunning: false);
    }

    void RestoreCountsAfterFailedResume(int stoppedSteps, (IList<JobStepId> Stopped, List<JobStepId> Completed) countedSteps, JobProgress? stoppedProgress)
    {
        State.Progress.StoppedSteps = stoppedSteps;
        (State.StoppedStepsCounted, State.CompletedStepsCounted) = countedSteps;
        if (stoppedProgress is null)
        {
            return;
        }

        State.Progress = stoppedProgress;

        // The write may have reached storage despite its failed acknowledgement. A fresh activation
        // must read the durable outcome rather than serve this activation's restored counters.
        DeactivateOnIdleDuringShutdown();
    }
}
