// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Monads;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.Placement;
using Orleans.Providers;
using Orleans.Utilities;

namespace Cratis.Choreography.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJob{TRequest}"/>.
/// </summary>
/// <typeparam name="TRequest">Type of request object that gets passed to job.</typeparam>
/// <typeparam name="TJobState">Type of state for the job.</typeparam>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Jobs)]
[ResourceOptimizedPlacement]
[JobStepPlanStorageSupport]
public abstract partial class Job<TRequest, TJobState> : Grain<TJobState>, IJob<TRequest>, IJobDrain
    where TRequest : class, IJobRequest
    where TJobState : JobState
{
    Dictionary<JobStepId, IJobStep>? _jobStepGrains;
    bool _hasLiveStepTracking;
    ObserverManager<JobStepId, IJobObserver>? _observers;
    ILogger<IJob> _logger = NullLogger<IJob>.Instance;
    IGrainTimer? _startStepsTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="Job{TRequest, TJobState}"/> class.
    /// </summary>
    protected Job()
    {
        ThisJob = this;
        Storage = null!;
    }

    enum HandleCompletionSuccess
    {
        AllStepsNotCompletedYet = 0,
        ClearedState = 1,
        NotClearedState = 2
    }

    /// <summary>
    /// Gets the <see cref="JobId"/> for this job.
    /// </summary>
    protected JobId JobId { get; private set; } = JobId.NotSet;

    /// <summary>
    /// Gets the <see cref="JobKey"/> for this job.
    /// </summary>
    protected JobKey JobKey { get; private set; } = JobKey.NotSet;

    /// <summary>
    /// Gets a value indicating whether to keep the persisted data after the job has completed.
    /// </summary>
    protected virtual bool KeepAfterCompleted => false;

    /// <summary>
    /// Gets a value indicating whether to keep the persisted data after the job has completed with failures.
    /// </summary>
    /// <remarks>
    /// Kept by default, so a failed run stays readable. A job that runs on a short cadence produces a new run every
    /// time it fails, and keeping each one accumulates a record per run for as long as the failure lasts - such a
    /// job reports its failures elsewhere and overrides this to let them go.
    /// </remarks>
    protected virtual bool KeepAfterCompletedWithFailures => true;

    /// <summary>
    /// Gets a value indicating whether the job's steps are prepared and started after <see cref="Start"/> has
    /// returned rather than as part of it.
    /// </summary>
    /// <remarks>
    /// A job whose step count scales with the size of the event store - one step per event source - pays a grain
    /// activation, a prepare and a start, each a storage write, for every one of them. Doing that inside
    /// <see cref="Start"/> bills the whole burst to whoever called it, and for an observer job that caller is
    /// Subscribe, with the client's registration call waiting behind it under a response timeout. Against a large
    /// enough store registration therefore timed out and took host startup down with it. Such a job overrides this
    /// to hand the work to its own next turn instead, so <see cref="Start"/> returns as soon as the job is durable
    /// and the steps come up behind it. The trade is that <see cref="Start"/> can no longer answer whether the
    /// steps started - failures are logged and handled by the job rather than returned to the caller.
    /// </remarks>
    protected virtual bool StartStepsInBackground => false;

    /// <summary>
    /// Gets the job as a Grain reference.
    /// </summary>
    protected IJob<TRequest> ThisJob { get; private set; }

    /// <summary>
    /// Gets the request associated with the job.
    /// </summary>
    protected TRequest Request => (State.Request as TRequest)!;

    /// <summary>
    /// Gets the underlying <see cref="JobsStorage"/> for the job's scope and namespace.
    /// </summary>
    protected JobsStorage Storage { get; private set; }

    /// <summary>
    /// Gets a value indicating whether all steps have been completed successfully.
    /// </summary>
    protected bool AllStepsCompletedSuccessfully => State.Progress.SuccessfulSteps == State.Progress.TotalSteps;

    bool ResumeNeedsAdmission =>
        State.Status is JobStatus.PreparingJob or JobStatus.PreparingSteps or JobStatus.StartingSteps ||
        (JobIsPrepared() && State.Status is JobStatus.Running or JobStatus.Stopped or JobStatus.Removing);

    bool ResumeNeedsStepReload => _jobStepGrains is null or { Count: 0 } || IsStaged || EffectiveMaxActiveSteps is not null;

    bool ShouldClearCompletedState => State.Status switch
    {
        JobStatus.Failed => false,
        JobStatus.CompletedWithFailures => !KeepAfterCompletedWithFailures,
        _ => !KeepAfterCompleted
    };

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Keep the Grain alive forever: Confirmed here: https://github.com/dotnet/orleans/issues/1721#issuecomment-216566448
        DelayDeactivation(TimeSpan.FromDays(365 * 5));

        _logger = ServiceProvider.GetService<ILogger<Job<TRequest, TJobState>>>() ?? new NullLogger<Job<TRequest, TJobState>>();
        _observers = new(
            TimeSpan.FromDays(365 * 3),
            ServiceProvider.GetService<ILogger<ObserverManager<JobStepId, IJobObserver>>>() ?? new NullLogger<ObserverManager<JobStepId, IJobObserver>>());

        JobId = this.GetPrimaryKey(out var keyExtension);
        JobKey = keyExtension!;
        ThisJob = GrainFactory.GetReference<IJob<TRequest>>(this);
        State.Type = ServiceProvider.GetRequiredService<IJobTypes>().GetForOrThrow(this.GetGrainType());
        Storage = ServiceProvider.GetRequiredService<IJobsStorage>()
            .GetFor(JobKey.Scope, JobKey.Namespace);

        if (JobIsPrepared())
        {
            _jobStepGrains = await GetIdAndGrainReferenceForNonCompletedJobSteps();
            await CountStepsFromStorageAfterActivation();
        }

        // Only a watchdog can settle a cold activation's hook; without one, dispatch keeps today's behavior.
        _resumeHookOwed = JobIsRunning() && WatchdogIsEnabled;
        if (State.Status is JobStatus.Running || IsStartupWhoseResumeHookIsOwed)
        {
            EnsureWatchdog(WatchdogStartupDelay);
        }
        if (State.DrainPending)
        {
            CompleteDrainAfterActivation();
        }
    }

    /// <inheritdoc/>
    public async Task<Result<StartJobError>> Start(TRequest request)
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        _logger.Starting();
        if (StartRefusedByState() is { } refused)
        {
            return refused;
        }

        // Nothing is changed or written before the owner has been asked: a refused job leaves no trace.
        if (await AnswerStartFromAdmission(request) is { } notAdmitted)
        {
            return notAdmitted;
        }

        State.Created = DateTimeOffset.UtcNow;
        State.Request = request;
        State.Details = GetJobDetails();

        // The execution begins here; its steps carry this change's position, so none may start unless it is durable.
        if (!await TryWriteStatusChanged(JobStatus.PreparingJob))
        {
            return StartJobError.Unknown;
        }

        if (StartStepsInBackground)
        {
            ScheduleStartOfSteps(request);
            return Result<StartJobError>.Success();
        }

        return await PrepareAndStartRunningAllStepsAndHandleFailureToPrepare(request);
    }

    /// <inheritdoc/>
    public async Task<Result<ResumeJobSuccess, ResumeJobError>> ResumeIfRunning()
    {
        if (State.Status is not JobStatus.Running)
        {
            using var scope = _logger.BeginJobScope(JobId, JobKey);
            _logger.NotResumingJobThatIsNotRunning(State.Status);
            return ResumeJobSuccess.JobIsNotRunning;
        }

        return await Resume();
    }

    /// <inheritdoc/>
    public async Task<Result<ResumeJobSuccess, ResumeJobError>> Resume()
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);

        // Stopped is written before workers report their stops. Only an already accounted execution may be
        // restored if the hook rejects this resume; stage preparation must not change this entry-time decision.
        var stoppedProgress = SnapshotStoppedProgress();
        try
        {
            // An execution that continues or begins again - a resume, or a rehydration after a silo went away - is
            // asked about once, before anything of it runs.
            return await AnswerResumeFromEntryState()
                ?? await AnswerResumeFromAdmission()
                ?? await AnswerResumeFromStatus()
                ?? await AnswerResumeFromSteps()
                ?? await BeginResumedExecution(stoppedProgress);
        }
        catch (Exception ex)
        {
            _logger.Failed(ex);
            return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.Unknown);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<StopJobError>> Stop()
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        try
        {
            if (State.Status is JobStatus.Failed)
            {
                return StopJobError.Unknown;
            }
            if (State.Status is JobStatus.CompletedSuccessfully or JobStatus.CompletedWithFailures)
            {
                return StopJobError.JobIsCompleted;
            }
            if (!JobIsRunning())
            {
                return StopJobError.JobIsNotRunning;
            }

            _logger.Stopping();

            // A stop that is not durable is not a stop: a restart would find the job running, so it is not reported as stopped.
            var written = await WriteOrUndo(() => StatusChanged(JobStatus.Stopped));
            if (written.TryGetException(out var writeException))
            {
                _logger.FailedStoppingJobBecauseItWasNotPersisted(writeException);

                // The write may have reached storage despite its failed acknowledgement. A fresh activation
                // must read the durable outcome rather than serve this activation's restored state.
                DeactivateOnIdleDuringShutdown();
                return StopJobError.CouldNotPersistStop;
            }

            _deactivateOnceStopFinalized = false;
            DisposeStageAdmissionTimer();
            await AccountForStepsThatNeverStarted();

            // Do not call OnStopped here because it should be called after all steps have been stopped
            await StopNonCompletedStepsAndEnsureCompletion(false);

            return Result<StopJobError>.Success();
        }
        catch (Exception e)
        {
            _logger.Failed(e);
            return StopJobError.Unknown;
        }
    }

    /// <inheritdoc/>
    public async Task<Result<RemoveJobError>> Remove()
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        try
        {
            if (State.Status is JobStatus.Failed)
            {
                return RemoveJobError.Unknown;
            }
            if (State.Status is JobStatus.CompletedSuccessfully or JobStatus.CompletedWithFailures)
            {
                return RemoveJobError.JobIsCompleted;
            }
            if (State.Status is JobStatus.Removing)
            {
                return await ContinueRemoval();
            }

            _logger.Removing();

            // A removal that is not durable must not stop steps or be reported as accepted.
            var written = await WriteOrUndo(() => StatusChanged(JobStatus.Removing));
            if (written.TryGetException(out var writeException))
            {
                _logger.FailedRemovingJobBecauseItWasNotPersisted(writeException);

                // The write may have reached storage despite its failed acknowledgement. A fresh activation
                // must read the durable outcome rather than serve this activation's restored state.
                DeactivateOnIdleDuringShutdown();
                return RemoveJobError.CouldNotPersistRemoval;
            }

            DisposeStageAdmissionTimer();
            await AccountForStepsThatNeverStarted();

            // Do not call OnStopped here because it should be called after all steps have been stopped
            await StopNonCompletedStepsAndEnsureCompletion(true);
            return Result<RemoveJobError>.Success();
        }
        catch (Exception e)
        {
            _logger.Failed(e);
            return RemoveJobError.Unknown;
        }
    }

    /// <summary>
    /// What needs to be done by the Job implementation before starting all prepared job steps.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    protected virtual Task OnBeforeStartingJobSteps() => Task.CompletedTask;

    /// <summary>
    /// What needs to be done by the Job implementation before resuming all prepared and non-completed job steps.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    protected virtual Task OnBeforeResumingJobSteps() => Task.CompletedTask;

    /// <summary>
    /// Ensures consumer initialization before steps are dispatched on start or resume.
    /// </summary>
    /// <param name="context">Whether an interrupted startup may have left initialization incomplete.</param>
    /// <returns>A task representing initialization. Failure prevents dispatch.</returns>
    /// <remarks>
    /// Runs after the existing start or resume hook. Make this hook idempotent: recovery can invoke it again.
    /// The start hook itself is never replayed. Ordinary resumes of running or stopped jobs pass false.
    /// </remarks>
    protected virtual Task OnBeforeDispatchingJobSteps(JobStepDispatchContext context) => Task.CompletedTask;

    /// <summary>
    /// What needs to be done by the Job implementation when all job steps are completed or job is being removed.
    /// </summary>
    /// <remarks>
    /// Job step being completed means either successfully completed, failed or partially failed.
    /// </remarks>
    /// <returns>A task representing the operation.</returns>
    protected virtual Task OnAllStepsCompleted() => Task.CompletedTask;

    /// <summary>
    /// What needs to be done by the Job implementation when one or more job steps failed to prepare.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    protected virtual Task OnFailedToPrepare() => Task.CompletedTask;

    /// <summary>
    /// What needs to be done by the Job implementation when running job steps are stopped or removed manually.
    /// </summary>
    /// <remarks>
    /// This is also called when a Job is being removed.
    /// OnStopped can be called on a Job two or more times. For instance if a Job is first stopped then removed later.
    /// It will first call OnStopped when the non-completed steps are stopped and then call it again when Remove is called.
    /// </remarks>
    /// <returns>A task representing the operation.</returns>
    protected virtual Task OnStopped() => Task.FromResult(Result.Success<JobError>());

    /// <summary>
    /// Check if the job can be resumed.
    /// </summary>
    /// <returns>True if it can, false if not.</returns>
    protected virtual Task<bool> CanResume() => Task.FromResult(true);

    /// <summary>
    /// Get the details for the job. This is for display purposes.
    /// </summary>
    /// <returns>The <see cref="JobDetails"/>.</returns>
    protected virtual JobDetails GetJobDetails() => JobDetails.NotSet;

    JobProgress? SnapshotStoppedProgress() =>
        State.Status is JobStatus.Stopped && !State.DrainPending && State.Progress.IsStopped
            ? State.Progress with { Stages = State.Progress.Stages.Select(stage => stage with { }).ToList() }
            : null;

    /// <summary>
    /// Schedule preparing and starting the job's steps on the grain's own next turn, so <see cref="Start"/> can
    /// return without its caller waiting for every step to come up.
    /// </summary>
    /// <param name="request">The <typeparamref name="TRequest"/> the job was started with.</param>
    /// <remarks>
    /// A grain timer rather than a detached task: the work reads and writes grain state, which is only safe on the
    /// activation. A zero due time means the tick is queued immediately and, being non-interleaving, runs once the
    /// activation is free - which is after <see cref="Start"/> has answered its caller.
    /// </remarks>
    void ScheduleStartOfSteps(TRequest request)
    {
        _startStepsTimer?.Dispose();
        _startStepsTimer = this.RegisterGrainTimer(
            async _ =>
            {
                _startStepsTimer?.Dispose();
                _startStepsTimer = null;

                // Stopping or removing the job between Start answering and this tick makes the steps moot.
                if (State.Status is not JobStatus.PreparingJob)
                {
                    _logger.NotStartingJobStepsForJobThatIsNoLongerPreparing(State.Status);
                    return;
                }

                var result = await PrepareAndStartRunningAllStepsAndHandleFailureToPrepare(request);
                if (result.TryGetError(out var error))
                {
                    _logger.FailedStartingJobStepsInBackground(error);
                }
            },
            new GrainTimerCreationOptions { DueTime = TimeSpan.Zero, Period = Timeout.InfiniteTimeSpan });
    }

    async Task<Result<StartJobError>> PrepareAndStartRunningAllStepsAndHandleFailureToPrepare(TRequest request)
    {
        var result = await PrepareAndStartRunningAllSteps(request);
        if (result.TryGetError(out var error) && error is StartJobError.CouldNotPrepareJobSteps)
        {
            await OnFailedToPrepare();
        }

        return result;
    }

    /// <summary>
    /// Finalizes a job that was asked to resume but has nothing left to run.
    /// </summary>
    /// <returns><see cref="ResumeJobSuccess.JobIsCompleted"/> when the job did finalize; otherwise a failure.</returns>
    /// <remarks>
    /// <see cref="HandleCompletion"/> deliberately leaves a <see cref="JobStatus.Stopped"/> job stopped, because a stopped
    /// job can be resumed later. This <em>is</em> that resume, and there is nothing left to resume: every step finished
    /// before or while the job was stopped - a client disconnecting mid catch-up leaves exactly that. Leaving it Stopped
    /// while reporting <see cref="ResumeJobSuccess.JobIsCompleted"/> told the caller the job had concluded when it never
    /// would: it never ran <see cref="OnAllStepsCompleted"/>, so whatever waited on it waited forever, and every later
    /// resume found the same stopped job and reported the same false success (Cratis/Chronicle#4363).
    /// The job is moved out of Stopped first so it completes like any other job whose steps are all accounted for.
    /// If it still does not complete, that is reported as a refusal rather than a success.
    /// </remarks>
    async Task<Result<ResumeJobSuccess, ResumeJobError>> FinalizeInsteadOfResuming()
    {
        if (State.Status is JobStatus.Stopped)
        {
            _logger.FinalizingStoppedJobWithNothingLeftToRun();
            if (!await TryWriteStatusChanged(JobStatus.Running))
            {
                return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.Unknown);
            }
        }

        var completion = await HandleCompletion();
        _ = await HandleCompletionResult(completion);
        if (completion.TryGetResult(out var completed) && completed is not HandleCompletionSuccess.AllStepsNotCompletedYet)
        {
            return ResumeJobSuccess.JobIsCompleted;
        }

        return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.JobCannotBeResumed);
    }

    /// <summary>
    /// Continues a removal that is already under way, finishing it when nothing is left to wait for.
    /// </summary>
    /// <returns>The result of continuing the removal.</returns>
    /// <remarks>
    /// Steps still tracked are told again, and their reports finish the removal. When none are left to report, a
    /// finalization that failed before - the stopped hook, clearing the job, or saving what was counted - runs again
    /// here, and a failure is answered as one, so the removal can be asked to continue again.
    /// </remarks>
    async Task<Result<RemoveJobError>> ContinueRemoval()
    {
        if (await StopNonCompletedStepsAndEnsureCompletion(true) is not { } completion)
        {
            return RemoveJobError.JobIsAlreadyBeingRemoved;
        }
        if (completion.TryGetError(out var error))
        {
            _logger.FailedFinalizingRemoval(error);
            return RemoveJobError.Unknown;
        }
        return RemoveJobError.JobIsAlreadyBeingRemoved;
    }

    /// <summary>
    /// Stops the steps that have not completed, or finishes the job's completion when there are none.
    /// </summary>
    /// <param name="removing">Whether the job is being removed.</param>
    /// <returns>The result of finishing the completion, or null when the steps finish it as they report.</returns>
    async Task<Result<JobError>?> StopNonCompletedStepsAndEnsureCompletion(bool removing)
    {
        if (_observers?.Count > 0)
        {
            // The job steps will eventually handle completion
            await NotifySteps(removing);
            return null;
        }

        // This state can occur when we are trying to stop or remove a Job that is persisted over a system shutdown.
        // Then we want to resubscribe all non-completed job steps
        if (State.Progress is { IsCompleted: false, IsStopped: false })
        {
            // Steps in a stage that has not started are not running, so there is nothing of theirs to stop.
            _jobStepGrains ??= await GetIdAndGrainReferenceForNonCompletedJobSteps();
            foreach (var (_, jobStepGrain) in StepsInCurrentStage())
            {
                await SubscribeJobStep(jobStepGrain.AsReference<IJobObserver>());
            }
        }

        if (_observers is null || _observers.Count == 0)
        {
            // All job steps should be in stopped state. Just HandleCompletion manually
            return await HandleCompletionResult(await HandleCompletion());
        }

        // The job steps will eventually handle completion
        await NotifySteps(removing);
        return null;
    }

    async Task<bool> HandleResumeJobStepResult(JobStepId jobStepId, Result<StartJobStepError> result, IJobStep jobStepGrain)
    {
        if (!result.TryGetError(out var error))
        {
            await TrySubscribeJobStep(jobStepId, jobStepGrain);
            return false;
        }
        if (error is StartJobStepError.AlreadyStarted)
        {
            _logger.JobStepWasAlreadyStarted(jobStepId, error);
            await TrySubscribeJobStep(jobStepId, jobStepGrain);
            return false;
        }
        if (error is StartJobStepError.Completed)
        {
            _logger.JobStepWasAlreadyStarted(jobStepId, error);
            return false;
        }
        _logger.FailedStartingJobStep(jobStepId, error);
        return true;
    }

    async Task TrySubscribeJobStep(JobStepId jobStepId, IJobStep jobStepGrain)
    {
        try
        {
            await SubscribeJobStep(jobStepGrain.AsReference<IJobObserver>());
        }
        catch (Exception ex)
        {
            _logger.FailedSubscribingToJobStep(ex, jobStepId);
        }
    }

    async Task<Result<HandleCompletionSuccess, JobError>> HandleCompletion()
    {
        try
        {
            if (CompletionNotReady() is { } notReady)
            {
                return notReady;
            }

            // When all steps have been stopped or completed
            if (State.Status is JobStatus.Stopped or JobStatus.Removing)
            {
                await OnStopped();
                if (State.Status is JobStatus.Stopped)
                {
                    // Persisted by the write that follows completion handling, together with the rest of the stop.
                    State.StopFinalized = true;
                }
            }

            // Don't go to Complete if the job is Stopped, because it can be resumed or removed in the future.
            if (State.Status is JobStatus.Stopped)
            {
                return HandleCompletionSuccess.AllStepsNotCompletedYet;
            }

            var onCompletedResult = await Complete();
            RecordCompletionStatus(onCompletedResult);

            // Removing retains its existing clear-only behavior.
            if (State.Status is JobStatus.Removing)
            {
                return await ClearRemovedJob();
            }

            _clearCompletedState = ShouldClearCompletedState;
            return await FinalizeCompletion();
        }
        catch (Exception ex)
        {
            _logger.Failed(ex);

            // Every step has reported, so nothing else will finish the removal until another Remove or a restart.
            ScheduleRemovalRetry();
            return JobError.UnknownError;
        }
    }

    Result<HandleCompletionSuccess, JobError>? CompletionNotReady()
    {
        if (_completionRetryTimer is not null)
        {
            return JobError.PersistStateError;
        }
        if (_completionFinalized || State.Status is JobStatus.CompletedSuccessfully or JobStatus.CompletedWithFailures or JobStatus.Failed)
        {
            return HandleCompletionSuccess.NotClearedState;
        }
        if (State.Progress is { IsCompleted: false, IsStopped: false })
        {
            return HandleCompletionSuccess.AllStepsNotCompletedYet;
        }

        // A stage that could not be asked about yet, or that waits for the resume hook, counts as stopped as it
        // stands, but the job is still running.
        if (IsWaitingToAdmitNextStage || IsHoldingNextStageForResumeHook)
        {
            return HandleCompletionSuccess.AllStepsNotCompletedYet;
        }

        return null;
    }

    async Task<Result<HandleCompletionSuccess, JobError>> ClearRemovedJob()
    {
        await ClearStateAsync();
        ClearStepNotificationRetries();
        ClearRemovalRetry();
        DeactivateOnIdleDuringShutdown();
        return HandleCompletionSuccess.ClearedState;
    }

    void RecordCompletionStatus(Catch result)
    {
        if (result.TryGetException(out var error))
        {
            StatusChanged(JobStatus.Failed, error);
        }
        else if (State.Status is not JobStatus.Removing)
        {
            StatusChanged(State.Progress.HasFailures ? JobStatus.CompletedWithFailures : JobStatus.CompletedSuccessfully);
        }
    }

    void DeactivateOnIdleDuringShutdown()
    {
        try
        {
            DeactivateOnIdle();
        }
        catch (InvalidOperationException)
        {
            // Grain activation may already be invalid during silo shutdown.
        }
    }

    async Task<Catch> Complete()
    {
        try
        {
            if (State.Status is JobStatus.CompletedSuccessfully or JobStatus.CompletedWithFailures or JobStatus.Failed)
            {
                return Catch.Success();
            }

            if (!AllStepsCompletedSuccessfully)
            {
                _logger.AllStepsNotCompletedSuccessfully();
            }
            await OnAllStepsCompleted();
            return Catch.Success();
        }
        catch (Exception ex)
        {
            _logger.FailedOnCompleted(ex, State.Id, State.Type);
            return ex;
        }
    }
}
