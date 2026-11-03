---
title: Workflow engine reference
description: Node, edge, wait, join and failure kinds, outlets, node configuration, statuses, trigger kinds, limits and cron syntax of the workflow engine.
---

Lookup tables for the workflow engine. The concepts are in [the model](index.md) and [the runtime](runtime.md).
Namespaces are `Cratis.Choreography.Workflows` and its children (`Graphs`, `Adapters`, `Waits`, `Triggers`, `Runs`); the
Chronicle types are in `Cratis.Choreography.Chronicle.Workflows`.

## Node kinds

| `WorkflowNodeKind` | Performed by | Configuration | Typical outlets |
| --- | --- | --- | --- |
| `Trigger` | The engine, with the trigger's payload as output | none | `next` |
| `Action` | An `IWorkflowNodePerformer` in a job step | adapter-defined | `next`, `failed`, `fallback` |
| `Decision` | An `IWorkflowNodePerformer` in a job step | adapter-defined | the decision's own, `failed`, `fallback` |
| `Wait` | The engine opens a wait | `WaitConfiguration` | `elapsed`, `resumed`, `cancelled`, `expired` |
| `Approval` | The engine opens an approval wait | `WaitConfiguration` | `approved`, `rejected`, `expired` |
| `ForEach` | The engine fans out | `ForEachConfiguration` | `each` |
| `Join` | The engine gathers the branches | `JoinConfiguration` | `completed`, `partial` |

A node's `Outlets` are what the author declared; the engine does not derive them from the kind. A node record is
`WorkflowNode(Id, Kind, Capability, Configuration, Outlets, Policy)`, an edge is `WorkflowEdge(From, Outlet, To)`.

## Node configuration

Configuration is JSON text in `WorkflowNodeConfiguration`, read with camelCase property names, enums as camelCase
strings and a `TimeSpan` as `[d.]hh:mm:ss`. It never holds a secret.

| Node | Property | Meaning |
| --- | --- | --- |
| `Wait` | `kind` | `signal` or `timer` (`WaitKind`) |
| `Wait` | `after` | How long a timer waits |
| `Wait` | `expiresAfter` | How long a signal or approval waits before it expires; zero waits forever |
| `Wait` | `signal` | The contract a signal wait accepts, `{"id":"payment-received","version":1}`; default is any |
| `Approval` | `expiresAfter` | As for a wait. An approval node is always an approval, whatever `kind` says |
| `ForEach` | `itemsFrom` | The node whose output holds the items |
| `ForEach` | `itemsProperty` | The property of that output holding the list |
| `ForEach` | `maxParallelism` | How many branch executions may be scheduled at once; values below 1 become 1 |
| `Join` | `forEach` | The `ForEach` it joins |
| `Join` | `mode` | `all`, `any` or `atLeast` |
| `Join` | `threshold` | How many branches must succeed for `atLeast` |

Every item in the list must be an object with a unique, non-empty string `key`; the key names the branch. A missing or repeated
key fails the `ForEach` with `InvalidOutput`.

## Node policy

`WorkflowNodePolicy(MaxAttempts, AttemptTimeout, RetryDelay, MaxVisits)`. `WorkflowNodePolicy.Default` is one attempt of at most ten
minutes, a 30 second retry delay and one visit. A node on a cycle needs `MaxVisits` above 1, or the graph is refused.

## Outlets

The twelve names in `WellKnownOutlets`:

| Outlet | Taken |
| --- | --- |
| `next` | As the ordinary way on |
| `failed` | When a node failed for good |
| `fallback` | When a node answered with an outlet it does not declare, or output that is not usable, and `fallback` is connected |
| `each` | Once per item by a `ForEach` |
| `completed` | By a join whose mode was satisfied |
| `partial` | By a join whose mode was not satisfied once every branch had ended |
| `resumed` | By a signal wait that was resumed |
| `elapsed` | By a timer wait once its time has come |
| `expired` | By a signal or approval wait that ran out of time |
| `approved` | By an approval that was given |
| `rejected` | By an approval that was refused |
| `cancelled` | By a signal wait that was called off |

## Failure kinds

| `WorkflowFailureKind` | Cause | Retried | Routed through |
| --- | --- | --- | --- |
| `Error` | The performer reported a failure or threw | while attempts remain | `failed` |
| `TimedOut` | The attempt ran past `AttemptTimeout` | while attempts remain | `failed` |
| `InvalidOutlet` | The performer answered with an undeclared outlet | no | `fallback`, else `failed` |
| `InvalidOutput` | Output was not JSON, too large, or not the shape needed | no | `fallback`, else `failed` |
| `PerformerMissing` | No performer is registered for the capability | no | `failed` |
| `VisitLimitExceeded` | The node was reached more often than `MaxVisits` | no | nothing: unrouted |

A failure with no connected outlet is unrouted. In the root branch it fails the run; in a `ForEach` branch it only ends the branch.
A retry becomes due `RetryDelay` after the failed attempt.

## Outcomes, waits and joins

| Type | Values |
| --- | --- |
| `WorkflowNodeOutcomeStatus` | `Succeeded` (leaves through an outlet), `Skipped` (the branch ends there, not a failure), `Failed`, `Ambiguous` (never retried) |
| `WaitKind` | `Signal` (resolved through `resumed` or `cancelled`, or `expired` when time runs out), `Timer` (leaves through `elapsed` when its time comes), `Approval` (resolved through `approved` or `rejected`, or `expired`) |
| `JoinMode` | `All` (every branch succeeded), `Any` (at least one), `AtLeast` (at least `threshold`) |

A skipped branch counts as succeeded for a join. A join fires as soon as its mode is satisfied, or once every branch has ended.
Its output is every branch's `key`, `status`, `outlet` and `output`, keyed by branch. A wait can be resolved or expired only while open:
whichever is recorded first wins.

## Statuses

| Type | Values |
| --- | --- |
| `WorkflowRunStatus` | `Running`, `Waiting`, `Succeeded`, `Failed`, `Cancelled`, `ReconciliationRequired` |
| `NodeExecutionStatus` | `Pending`, `Scheduled`, `Waiting`, `Succeeded`, `Failed`, `Skipped`, `Unreachable`, `Cancelled`, `ReconciliationRequired`, `RetryPending` |

## Structure violations

`StructuralAdmissionPolicy` refuses a graph with any of these `WorkflowStructureViolationKind` values: `NoTrigger`, `MoreThanOneTrigger`,
`UnknownNode`, `UndeclaredOutlet`, `ForEachWithoutSingleJoin`, `NestedForEachNotSupported`, `UnboundedCycle` (a cycle through a node
with `MaxVisits` of 1 or less), `InvalidConfiguration` and `DuplicateNode`.

## Triggers and limits

| `WorkflowTriggerKind` | Started by |
| --- | --- |
| `Manual` | A person or code, through `IWorkflows.Start` |
| `Signal` | A signal from outside |
| `StageHook` | A state machine entering a state, through `StartWorkflow` |
| `Schedule` | A schedule |

The engine records the kind and does not branch on it. `WorkflowData`, which carries payloads, inputs, outputs and signal
payloads, is JSON of at most 32 KiB (`WorkflowData.MaxBytes`); larger content is stored by the adapter and referred to.
Run input is `{ "trigger": { payload, subject, correlation }, "nodes": { id: output }, "item": ... }`.

## Cron expressions

`CronExpression.Parse` reads five space-separated fields, evaluated in UTC: minute (0-59), hour (0-23), day of month (1-31),
month (1-12) and day of week (0-6, with 7 also Sunday). Each field is `*`, a number, a range `a-b`, a step `*/n` or `a-b/n`, or a
comma-separated list of those. Names such as `MON` are not supported. When both day of month and day of week are restricted, either
matching is enough, as in classic cron. Anything else throws `InvalidCronExpression`.

| Expression | Fires |
| --- | --- |
| `0 6 * * 1-5` | 06:00 UTC on weekdays |
| `*/15 * * * *` | Every 15 minutes |
| `0 0 1 * *` | Midnight UTC on the first of each month |

## Facts

The journal records the types in `WorkflowRunFacts.Types`, named by their bare type name: `WorkflowRunAdmitted`,
`WorkflowNodeScheduled`, `WorkflowNodeOperationCompleted`, `WorkflowNodeSucceeded`, `WorkflowNodeSkipped`,
`WorkflowNodeAttemptFailed`, `WorkflowNodeFailed`, `WorkflowNodeOutcomeAmbiguous`, `WorkflowNodesUnreachable`,
`WorkflowBranchesFannedOut`, `WorkflowBranchesJoined`, `WorkflowWaitOpened`, `WorkflowWaitResolved`, `WorkflowWaitCancelled`,
`WorkflowRunSucceeded`, `WorkflowRunFailed`, `WorkflowRunCancelled`, `WorkflowRunNeedsReconciliation`, `WorkflowNodeInputPinned` and
`WorkflowJobStopped`.
