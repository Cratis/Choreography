// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Choreography.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    /// <summary>
    /// Answers a resume that the job's current state settles before anything of it is asked or reloaded.
    /// </summary>
    /// <returns>The answer, or null when the resume goes on.</returns>
    async Task<Result<ResumeJobSuccess, ResumeJobError>?> AnswerResumeFromEntryState()
    {
        if (State.Status is JobStatus.Failed)
        {
            return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.Unknown);
        }

        // A drain is not settled yet; nothing of the job may run until it is.
        if (State.DrainPending)
        {
            _logger.NotResumingWhileDrainIsPending();
            return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.JobCannotBeResumed);
        }

        if (_startStepsTimer is not null)
        {
            return ResumeJobSuccess.JobAlreadyRunning;
        }

        // The same contradiction can be left in StartingSteps: the last step reported before the status moved on to
        // Running. There is nothing left to run, so finalize it without asking whether it may run again.
        if (State.Status is JobStatus.StartingSteps && State.Progress is { TotalSteps: > 0, IsCompleted: true })
        {
            _logger.FinalizingJobLeftStartingStepsAfterAllStepsCompleted();
            _ = await HandleCompletionResult(await HandleCompletion());
            return ResumeJobSuccess.JobIsCompleted;
        }

        return null;
    }

    /// <summary>
    /// Asks whether an execution that continues or begins again may run, once, before anything of it runs.
    /// </summary>
    /// <returns>The answer when the execution is refused or the question failed, or null when the resume goes on.</returns>
    async Task<Result<ResumeJobSuccess, ResumeJobError>?> AnswerResumeFromAdmission()
    {
        if (!ResumeNeedsAdmission)
        {
            return null;
        }

        switch (await Admit(State.Request, "Resume"))
        {
            case Admission.Refused:
                return await RefuseResume();
            case Admission.Failed:
                return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.Unknown);
        }

        return null;
    }

    /// <summary>
    /// Answers a resume from the job's status, recovering or finalizing what the status shows was left unfinished.
    /// </summary>
    /// <returns>The answer, or null when the job is prepared and stopped and resumes from its steps.</returns>
    async Task<Result<ResumeJobSuccess, ResumeJobError>?> AnswerResumeFromStatus()
    {
        // A job can be left persisted as running with every one of its steps already accounted for: the
        // progress for the last step is written before the status that follows from it, so a silo that stops
        // in between - or a completion handler that fails - leaves a state that contradicts itself. Nothing
        // revisited it, and reporting it as already running kept it that way: the job stayed Running forever
        // and its observer could never resubscribe (#3944). Resume is where both the kernel's rehydration and
        // an observer's subscribe reach the job, so finalize it here rather than leaving it wedged.
        if (State.Status is JobStatus.Running && State.Progress.IsCompleted)
        {
            _logger.FinalizingJobLeftRunningAfterAllStepsCompleted();
            _ = await HandleCompletionResult(await HandleCompletion());
            return ResumeJobSuccess.JobIsCompleted;
        }

        if (State.Status is JobStatus.Running && await ResumeStagesLeftBetween() is { } resumedStages)
        {
            return resumedStages;
        }

        return await RecoverFromStatus();
    }

    async Task<Result<ResumeJobSuccess, ResumeJobError>?> RecoverFromStatus() =>
        State.Status switch
        {
            JobStatus.PreparingJob or JobStatus.PreparingSteps => await RecoverStepPreparation(),
            JobStatus.StartingSteps => await RecoverStepStartup(),
            JobStatus.Running => await ResumeInterruptedSteps(),
            JobStatus.CompletedSuccessfully or JobStatus.CompletedWithFailures => ResumeJobSuccess.JobIsCompleted,
            _ => null
        };
}
