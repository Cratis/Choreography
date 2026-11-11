---
title: Answers that arrive later, capability gates and reconciliation
description: How a performer suspends its node until it is completed by key or expires, how to bound concurrent calls per capability, and how to resume a run stopped on an ambiguous outcome.
---

A performer that calls an external system sometimes cannot answer when the call returns: a question posted to a chat tool, an
approval card, a booking a partner confirms hours later. And it sometimes cannot tell whether its call worked at all. This page
describes the three pieces of the workflow engine built for that work: suspending a node until it is completed by key, limiting
concurrent calls per capability, and resuming a run that stopped on an ambiguous outcome.

[Stage](https://github.com/Cratis/Stage) runs every Screenplay ability invocation as one workflow run whose node work is an
`IWorkflowNodePerformer` for the capability `ability:<System>.<Ability>@<major>`. How Stage names, retries and reconciles
invocations is described in the Stage invocation documentation, tracked in
[Cratis/Stage#1067](https://github.com/Cratis/Stage/issues/1067). This page is the engine side of that contract, and it applies
to any performer.

## Suspend a node until it is completed by key

A performer answers `WorkflowNodeOutcome.Suspended(key, expiresAfter)` when it accepted the work and will answer later.

```csharp
public async Task<WorkflowNodeOutcome> Perform(WorkflowNodeInvocation invocation, CancellationToken cancellationToken)
{
    await chat.PostQuestion(invocation.IdempotencyKey.Value, invocation.Input, cancellationToken);
    return WorkflowNodeOutcome.Suspended(invocation.IdempotencyKey.Value, TimeSpan.FromHours(4));
}
```

The node leaves its worker and waits, like a `wait` node: the engine records an open wait of kind `WaitKind.Completion` with the
key as its correlation, and nothing holds a job, a thread or a timer while it waits. The key is whatever the answer will carry.
For an extension invocation, use the invocation identity, `invocation.IdempotencyKey`, which is the same for every attempt.

The wait ends in one of two ways, and whichever is recorded first wins.

- **Completed by key.** Whoever has the answer completes the wait through an outlet the node declares, with the answer as the
  node's output. The node succeeds through that outlet, and the run continues from it.
- **Expired.** After `expiresAfter` the wait resolves through the `expired` outlet with no output. A node that suspends
  should declare an `expired` outlet and route it. A zero `expiresAfter` never expires.

Suspending is not a retry and does not use up an attempt. Suspending without a key fails the attempt as invalid output.

### Complete by key

`IWorkflowCompletions.Complete` finds the open completion waits suspended under a key in a scope and completes them.

```csharp
var completed = await completions.Complete(
    scope,
    new WorkflowCompletionKey(invocationKey),
    "answered",
    """{"answer":"yes"}""",
    actor);
```

It returns how many waits it completed. Two things to know:

- The lookup uses the `OpenWorkflowWait` read model, which can lag the run. An answer that arrives before the suspension is
  visible completes nothing and is not remembered, so the caller retries while the result is zero and the invocation is not over.
- A caller that already holds the run and the execution skips the lookup. The wait identity is derived from them
  (`WaitHandling.WaitIdFor`), so `IWorkflowRun.ResolveWait(waitId, outlet, payload, actor)` completes it directly. A wait that was
  completed or has expired is not completed again, so repeating the call is safe.

A completion must leave through an outlet the node declares and carry JSON within `WorkflowData.MaxBytes`; anything else is
ignored, and the wait stays open.

## Limit concurrent calls per capability

Every node that is performing occupies a worker slot on the silo. A slow external system can therefore hold every slot, and
every other workflow waits behind it. `WorkflowsOptions.CapabilityLimits` bounds the performer calls of a capability that are in
flight on a silo at once.

```csharp
services.AddCratisChoreographyWorkflows(options =>
{
    options.CapabilityLimits["ability:Slack.*"] = 4;        // one gate shared by every Slack ability
    options.CapabilityLimits["ability:Billing.Charge"] = 1; // an exact rule beats a prefix rule
});
```

A rule is a capability id, or a prefix ending in `*`. The longest matching prefix wins, an exact rule beats any prefix, and a
capability no rule covers is not limited. The version of the capability is not part of the rule. A call waits for room inside the
attempt's timeout. If none comes, the attempt ends as `TimedOut` without the performer being called, and the node's retry policy
applies.

What the engine does not do, and Stage owns:

- **Rate limits.** A gate counts calls in flight, not calls per second. How often a system may be called is a property of that
  system's contract, and Stage reads it from deployment configuration.
- **Limits across silos.** Each silo has its own gates, so a cluster of three silos allows three times the limit. Divide a
  system's allowance by the number of silos when you configure it, or enforce a cluster-wide limit in Stage.
- **Keeping a waiting call from holding a slot.** A call waiting for room holds its worker slot until it gets room or times out.
  Keep a gate's limit near the worker concurrency so that one system cannot take every slot, and give slow systems the longer
  timeout through the node's policy.

## Resume a run stopped on an ambiguous outcome

A performer that cannot tell whether its effect happened answers `WorkflowNodeOutcome.Ambiguous(reason)`. The engine never retries
that node. The execution becomes `ReconciliationRequired`, everything that follows it is recorded as unreachable, and the run ends
as `ReconciliationRequired`.

`IWorkflowRun.GetSnapshot()` gives a person what they need to find out what happened. For each execution it carries the status, the
last attempt, the reason the performer gave (`NodeExecutionSnapshot.Reason`) and the invocation identity
(`NodeExecutionSnapshot.IdempotencyKey`), the key the performer deduplicated on. Ask the external system what it did for that key.

Then record what happened with `IWorkflowRun.Reconcile(execution, outcome, actor)`:

| Chosen outcome | What the run does |
| --- | --- |
| `WorkflowNodeOutcome.Succeeded(outlet, output)` | The node succeeds through that outlet, with that output. The nodes that followed it become reachable again and the run continues. |
| `WorkflowNodeOutcome.Failed(reason)` | The node fails and is routed like any failure, without a retry. |
| `WorkflowNodeOutcome.Skipped(reason)` | The node is skipped and the run continues from there. |

The engine records `WorkflowNodeReconciled` with the attempt that was unknown and the actor who chose the outcome, followed by the
chosen outcome as an ordinary fact. The run is `Running` again, and any other ambiguous execution still stops it.

Reconciliation records nothing, and is safe to repeat, when:

- the execution is not waiting for reconciliation, including when it was already reconciled;
- the outcome is `Ambiguous` or `Suspended`, which a person cannot choose;
- a chosen success names an outlet the node does not declare, or its output is not bounded JSON;
- the run was cancelled;
- the execution is inside a `for-each` branch. Reconciling branch executions is not supported, because the join has already
  gathered the branch.

## Rolling upgrades

`IWorkflowRun.Reconcile` is a new grain method, so `IWorkflowRun` is at `[Version(1)]`; see
[Mixed-release clusters](../upgrading/mixed-release-clusters.md). Do not call it until every silo of the cluster runs a release
that has it. `WorkflowNodeOutcomeStatus.Suspended` and `WaitKind.Completion` are new enum values that a silo of an earlier release
does not know, so hold back performers that suspend until the whole cluster has upgraded.
