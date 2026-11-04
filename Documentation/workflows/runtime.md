---
title: How workflow runs stay durable
description: How the workflow engine rebuilds a run from its journal, runs node work as jobs, keeps time without in-memory timers and maps scopes onto tenants.
---

A process that waits two days for an approval cannot keep its place in memory. This page explains what the engine
does so that a run picks up exactly where it stopped, whichever silo it lands on. None of it is code you write, but you need
it to reason about retries, restarts and the two delivery guarantees you are responsible for.

## Two grains run the engine

- **`IWorkflow`**, one per workflow and keyed by scope and workflow id, resolves the active definition, applies the
  [admission checks](index.md#definitions-and-admission) and admits the run.
- **`IWorkflowRun`**, one per run and keyed by scope and run id, is the durable process.

A run's id is derived from the workflow and the trigger's deduplication key. A trigger delivered twice finds the run the first
delivery started, so a retried HTTP call or a replayed event never starts a second run. The scope is part of the grain
key, so nothing a caller sends can move a run to another scope.

## The run's state is its journal

A run grain keeps no state of its own. Everything that happened to it is a fact, recorded in an `IWorkflowRunJournal`:
`WorkflowRunAdmitted`, `WorkflowNodeScheduled`, `WorkflowNodeSucceeded`, `WorkflowNodeAttemptFailed`,
`WorkflowWaitOpened`, `WorkflowWaitResolved`, `WorkflowBranchesFannedOut`, `WorkflowBranchesJoined`,
`WorkflowRunSucceeded`, `WorkflowRunFailed`, `WorkflowRunCancelled` and the rest of `WorkflowRunFacts.Types`.
Each time the grain activates it loads its facts, and the pure `WorkflowCoordinator` folds them into the state it
decides from. Every call the grain answers follows the same three steps:

1. The coordinator decides from the state which facts to record and which work to schedule.
2. The grain appends the facts, naming the tail of the journal it decided from.
3. Only when they are recorded does it start the work.

If another activation, or a call racing a restart, appended in the meantime, the append is refused. The grain loads the
journal again and decides once more; losing twice throws `WorkflowRunCouldNotBeRecorded`. So two activations of one run can
never both record a decision made from the same history, and a call that is repeated records nothing it already recorded.

The facts are a contract. Their type names and JSON shapes stay as they are, because stores already hold them: a
new fact is a new record, and an existing one is never renamed or reshaped.

`Cratis.Choreography` ships a journal on Orleans grain storage (`GrainStorageWorkflowRunJournal`, using the provider named
`workflows`), so the engine works without Chronicle. A run is then one grain-state document with an ETag, which suits runs of
moderate length. For a long-lived run with very many facts, [keep the journal in Chronicle](chronicle-journal-and-triggers.md#keep-the-journal-in-chronicle).

## Node work runs as jobs

When the coordinator schedules work, the run starts a `WorkflowWaveJob`: one job per wave of node executions that were
ready together, and one job step (`WorkflowNodeStep`) per execution. The job identity is derived from the run and the wave,
so a start that is repeated finds the job it started. If a wave never started, `Resume` reschedules scheduled work whose job is not running.

- A performer that fails, throws, runs past `AttemptTimeout` or does not exist is a business failure, reported to the run as a fact.
  The step itself succeeds. It fails only when the report could not reach the run, so the job's failure handling covers exactly
  the case where an outcome would be lost.
- A performer that implements `IStagedWorkflowNodePerformer` runs each operation as its own step in its own stage. An
  operation is recorded before the next starts, and an operation that does not succeed ends the node there.
- Wave jobs are not kept after they complete. The history lives in the journal.

Nothing is repeated that the run has recorded. A retry reuses the input pinned when the node was scheduled and the same
idempotency key, `WorkflowNodeInvocation.IdempotencyKey`, so a performer can deduplicate its external effect. Because a restart
can also invoke a performer twice for one attempt, deduplicating on that key is your half of the guarantee. When a
performer cannot tell whether its effect happened, it answers `Ambiguous`: the node is never retried, and the run ends as
`ReconciliationRequired` for a person to settle.

Cancelling a run (`IWorkflowRun.Cancel`) cancels its open waits and owes a stop to every wave job that held scheduled work.
The stop is retried until it is acknowledged. An outcome that arrives after cancellation is still recorded, because it
is a fact about the outside world, but nothing is planned from it.

## Time is recorded, not remembered

A wait's deadline and a retry's due time follow from the recorded facts. A failed attempt that will be retried records
only the failure; the retry is due `RetryDelay` later and becomes a fact when it starts. Nothing depends on a timer that
lives only in memory.

- While a run has a deadline or a retry pending, its grain keeps a grain timer and a reminder so it acts when the time
  comes (`WorkflowsOptions.WakeRunsItself`).
- A sweep asks every unfinished run of every known scope whether it has anything to do: expire waits that are due, start
  retries whose time has come, and take forward a run that has been idle too long or owes a stop. The runs to ask are held in a
  small index per scope, which the runs maintain themselves.

Reminders in the co-hosted silo are in memory, so they do not survive a full cluster restart; the sweep does. After a full
restart the sweep knows only the scopes its silo has served since, those in `WorkflowsOptions.Scopes` and those an
`IWorkflowScopeSource` names. Register an `IWorkflowScopeSource` that lists every scope, every tenant for example, so a run
waiting in a scope nothing has touched is found. The jobs recovery asks the same scopes when it restarts interrupted wave jobs.

## Scope maps onto your tenants

Workflows and runs are keyed by an opaque `WorkflowScope` and an id, written as `{scope}/{id}`. The engine never
interprets a scope. A host with tenants says what a scope is by implementing `IWorkflowExecutionScopes`:

- `Enter(scope)` establishes the context work for the scope runs in, which is the tenant;
- `JobsManagerFor(scope)` and `ScopeOf(key)` map the scope onto the jobs manager that holds its wave jobs and back.

The default uses the `ITenancy` registered with the silo when there is one, and keeps the wave jobs under the jobs scope
`workflows` (`WorkflowsOptions.JobsScope`) in a namespace that is the scope. [Tenancy](../tenancy.md) explains the tenant
model. To run node work as a particular principal, register an `IWorkflowNodeExecutionContext`: its `Begin` is called around every
node's work.

## Next steps

[Host and start](host-and-start.md) shows the code you write: performers, definitions and starting a run.
