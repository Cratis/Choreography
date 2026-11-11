// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Definitions;
using Cratis.Choreography.Workflows.Execution;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Journal;
using Cratis.Choreography.Workflows.Runs.Coordination;
using Cratis.Choreography.Workflows.Scopes;
using Cratis.Choreography.Workflows.Sweeping;
using Cratis.Choreography.Workflows.Triggers;
using Cratis.Choreography.Workflows.Waits;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Workflows.Runs;

/// <summary>
/// Represents an implementation of <see cref="IWorkflowRun"/>.
/// </summary>
/// <param name="journal">The <see cref="IWorkflowRunJournal"/> the run's facts are read from and appended to.</param>
/// <param name="scopes">The <see cref="IWorkflowExecutionScopes"/> that say where the run's wave jobs live.</param>
/// <param name="knownScopes">The <see cref="WorkflowScopes"/> the run's scope is remembered in.</param>
/// <param name="options">The <see cref="WorkflowsOptions"/>.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
/// <param name="logger">The logger.</param>
/// <remarks>
/// <para>
/// It keeps no Orleans state of its own: on activation it reads the run's facts from the journal and folds them,
/// and every call appends what the coordinator decided with the tail it decided from - so two activations, or a call
/// racing a restart, can never both record a decision made from the same history. Losing that race loads the
/// journal again and decides once more; losing it twice throws. Work is scheduled only after it is recorded, under a wave
/// job identity derived from the run and the wave, so a start that is repeated finds the job it started.
/// </para>
/// <para>
/// Timers are not facts. When a retry or a wait deadline is due follows from the facts, and the grain wakes itself for the
/// next one - a timer while it is active, a reminder when it is not - while the sweep asks it to act on whatever is due,
/// so nothing is lost when a silo goes away.
/// </para>
/// </remarks>
[GrainType("cratis-orleans-workflow-run")]
public class WorkflowRunGrain(
    IWorkflowRunJournal journal,
    IWorkflowExecutionScopes scopes,
    WorkflowScopes knownScopes,
    IOptions<WorkflowsOptions> options,
    TimeProvider timeProvider,
    ILogger<WorkflowRunGrain> logger) : Grain, IWorkflowRun, IRemindable
{
    const string WakeReminder = "cratis-orleans-workflow-due";
    static readonly TimeSpan _reminderPeriod = TimeSpan.FromMinutes(1);

    WorkflowRunKey _key = null!;
    WorkflowRunState _state = null!;
    WorkflowJournalPosition _tail = WorkflowJournalPosition.BeforeFirst;
    DateTimeOffset _lastActivity = DateTimeOffset.MinValue;
    bool? _indexed;
    IGrainTimer? _wake;
    IGrainReminder? _reminder;

    DateTimeOffset Now => timeProvider.GetUtcNow();

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _key = WorkflowRunKey.Parse(this.GetPrimaryKeyString());
        knownScopes.Remember(_key.Scope);
        await Load();
        await Settle();
    }

    /// <inheritdoc/>
    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _wake?.Dispose();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<WorkflowAdmission> Admit(WorkflowDefinitionSnapshot definition, WorkflowTrigger trigger)
    {
        await Execute(state => WorkflowCoordinator.Admit(_key, state, definition, trigger, Now));
        return WorkflowAdmission.To(_key.Run);
    }

    /// <inheritdoc/>
    public Task<bool> TryStartInvocation(NodeExecutionKey execution, WorkflowAttempt attempt) => Task.FromResult(
        !_state.IsOver && _state.Executions.TryGetValue(execution, out var recorded)
        && recorded.Status == NodeExecutionStatus.Scheduled && recorded.Attempt == attempt);

    /// <inheritdoc/>
    public Task CompleteNode(NodeExecutionKey execution, WorkflowAttempt attempt, WorkflowNodeOutcome outcome) =>
        Execute(state => WorkflowCoordinator.CompleteNode(_key, state, execution, attempt, outcome, Now));

    /// <inheritdoc/>
    public Task FailAttempt(NodeExecutionKey execution, WorkflowAttempt attempt, WorkflowFailureKind kind, WorkflowFailureReason reason) =>
        Execute(state => WorkflowCoordinator.FailAttempt(_key, state, execution, attempt, kind, reason, Now));

    /// <inheritdoc/>
    public Task CompleteOperation(NodeExecutionKey execution, WorkflowAttempt attempt, WorkflowOperationName operation, WorkflowData output) =>
        Execute(state => WorkflowCoordinator.CompleteOperation(_key, state, execution, attempt, operation, output, Now));

    /// <inheritdoc/>
    public Task<WorkflowData> GetOperationOutput(NodeExecutionKey execution, WorkflowAttempt attempt, WorkflowOperationName operation) =>
        Task.FromResult(_state.Operations.TryGetValue((execution, attempt, operation), out var output) ? output : WorkflowData.Empty);

    /// <inheritdoc/>
    public Task ResolveWait(WorkflowWaitId wait, OutletName outlet, WorkflowData payload, WorkflowActor actor) =>
        Execute(state => WorkflowCoordinator.ResolveWait(_key, state, wait, outlet, payload, actor, Now));

    /// <inheritdoc/>
    public Task Reconcile(NodeExecutionKey execution, WorkflowNodeOutcome outcome, WorkflowActor actor) =>
        Execute(state => WorkflowCoordinator.Reconcile(_key, state, execution, outcome, actor, Now));

    /// <inheritdoc/>
    public Task ExpireDueWaits(DateTimeOffset now) =>
        Execute(state => WorkflowCoordinator.ExpireDueWaits(_key, state, now));

    /// <inheritdoc/>
    public Task StartDueRetries(DateTimeOffset now) =>
        Execute(state => WorkflowCoordinator.StartDueRetries(_key, state, now));

    /// <inheritdoc/>
    public async Task Tend(DateTimeOffset now)
    {
        await ExpireDueWaits(now);
        await StartDueRetries(now);
        if (ShouldResume(now))
        {
            await Resume();
        }
    }

    /// <inheritdoc/>
    public async Task Resume()
    {
        await Load();
        await StopJobs();
        if (!_state.IsAdmitted || _state.IsOver)
        {
            await Settle();
            return;
        }

        var jobsManager = JobsManager();
        var running = (await jobsManager.GetJobsOfType<IWorkflowWaveJob, WorkflowWaveRequest>())
            .Where(job => job.IsPreparingOrRunning)
            .Select(job => new WorkflowJobReference(job.Id.Value.ToString()))
            .ToList();
        await Execute(state => WorkflowCoordinator.Resume(_key, state, running, Now));
    }

    /// <inheritdoc/>
    public Task Cancel(WorkflowFailureReason reason) =>
        Execute(state => WorkflowCoordinator.Cancel(_key, state, reason, Now));

    /// <inheritdoc/>
    public Task<WorkflowRunSnapshot> GetSnapshot() => Task.FromResult(WorkflowRunSnapshot.Of(_state));

    /// <inheritdoc/>
    public Task ReceiveReminder(string reminderName, TickStatus status) => Tend(Now);

    IJobsManager JobsManager()
    {
        var key = scopes.JobsManagerFor(_key.Scope);
        return GrainFactory.GetJobsManager(key.Scope, key.Namespace);
    }

    async Task Load()
    {
        var snapshot = await journal.Load(_key);
        _state = WorkflowRunFold.Replay(_key.Run, snapshot.Facts.Select(fact => (fact.Fact, fact.Occurred)));
        _tail = snapshot.Tail;
        _lastActivity = snapshot.Facts.Count > 0 ? snapshot.Facts[^1].Occurred : DateTimeOffset.MinValue;
    }

    async Task Execute(Func<WorkflowRunState, CoordinatorDecision> decide, bool deliverStops = true)
    {
        for (var attempt = 1; ; attempt++)
        {
            var decision = decide(_state);
            if (decision.IsEmpty)
            {
                if (deliverStops)
                {
                    await StopJobs();
                }

                await Settle();
                return;
            }

            if (decision.Events.Any())
            {
                var appended = await journal.Append(_key, _tail, [.. decision.Events]);
                if (ShouldRetryAppend(appended, attempt))
                {
                    logger.RunMovedOn(_key.Run.Value);
                    await Load();
                    continue;
                }

                _tail = appended.Tail;
                _state = decision.State;
                _lastActivity = Now;
            }

            await StartWave(decision);
            if (deliverStops)
            {
                await StopJobs();
            }

            await Settle();
            return;
        }
    }

    bool ShouldResume(DateTimeOffset now) =>
        _state.IsAdmitted && (_state.PendingJobStops.Count > 0 || (!_state.IsOver && now - _lastActivity > options.Value.IdleBeforeResume));

    bool ShouldRetryAppend(WorkflowJournalAppendResult result, int attempt)
    {
        if (result.IsSuccess)
        {
            return false;
        }
        if (result.HasConflict && attempt == 1)
        {
            return true;
        }
        throw new WorkflowRunCouldNotBeRecorded(_key, result.HasConflict ? "the run moved on twice while it was being decided" : result.Problems);
    }

    async Task StartWave(CoordinatorDecision decision)
    {
        if (!decision.ToSchedule.Any())
        {
            return;
        }

        var job = CoordinatorDecision.JobFor(_key.Run, decision.Wave);
        var started = await JobsManager().Start<IWorkflowWaveJob, WorkflowWaveRequest>(
            new JobId(Guid.Parse(job.Value)),
            new WorkflowWaveRequest(_key.ToString(), decision.Wave, [.. decision.ToSchedule]));

        // The work is recorded as scheduled either way: a wave that did not start is found by Resume,
        // which reschedules whatever is scheduled into a job that is not running.
        if (started.TryGetError(out var error))
        {
            logger.WaveDidNotStart(_key.Run.Value, decision.Wave, error.ToString());
        }
    }

    async Task StopJobs()
    {
        foreach (var job in _state.PendingJobStops.ToList())
        {
            var stopped = await JobsManager().Stop(new JobId(Guid.Parse(job.Value)));

            // A job that already completed or is not running has nothing left to stop; any other failure leaves
            // the stop pending so the delivery is retried instead of being acknowledged as stopped.
            if (stopped.TryGetError(out var error) && error is not (StopJobError.JobIsCompleted or StopJobError.JobIsNotRunning))
            {
                throw new InvalidOperationException($"Stopping job '{job.Value}' failed: {error}");
            }

            await Execute(
                state =>
                {
                    var work = new CoordinatorWork(_key, state, Now);
                    work.Record(new WorkflowJobStopped(job));
                    return work.Decide();
                },
                deliverStops: false);
        }
    }

    /// <summary>
    /// Brings what lives outside the facts in line with them - the scope's index of active runs, and the wake-up for the next deadline.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    async Task Settle()
    {
        await SyncIndex();
        await ScheduleWake();
    }

    async Task SyncIndex()
    {
        var shouldBeIndexed = _state.IsAdmitted && !(_state.IsOver && _state.PendingJobStops.Count == 0);
        if (!_state.IsAdmitted || shouldBeIndexed == _indexed)
        {
            return;
        }

        try
        {
            var index = GrainFactory.GetGrain<IWorkflowRunIndex>(_key.Scope.Value);
            if (shouldBeIndexed)
            {
                await index.Add(_key.Run);
            }
            else
            {
                await index.Remove(_key.Run);
            }

            _indexed = shouldBeIndexed;
        }
        catch (Exception exception)
        {
            logger.IndexCouldNotBeUpdated(exception, _key.Run.Value);
        }
    }

    async Task ScheduleWake()
    {
        if (!options.Value.WakeRunsItself)
        {
            return;
        }

        var next = _state.IsOver ? null : _state.NextDueAt;
        _wake?.Dispose();
        _wake = null;
        if (next is null)
        {
            if (_reminder is not null)
            {
                await this.UnregisterReminder(_reminder);
                _reminder = null;
            }

            return;
        }

        var delay = next.Value - Now;
        if (delay < TimeSpan.Zero)
        {
            delay = TimeSpan.Zero;
        }

        _wake = this.RegisterGrainTimer(() => Tend(Now), new GrainTimerCreationOptions(delay + TimeSpan.FromMilliseconds(50), Timeout.InfiniteTimeSpan) { Interleave = false });
        _reminder = await this.RegisterOrUpdateReminder(WakeReminder, delay > _reminderPeriod ? delay : _reminderPeriod, _reminderPeriod);
    }
}
