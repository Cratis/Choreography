---
title: Choosing a building block
description: Decide between a job, a workflow, a state machine, a worker or a plain Orleans grain for a piece of long-lived behavior, with the cases where each is wrong.
---

Cratis Choreography gives you four building blocks for behavior that outlives a request, and they overlap enough to be
confusing. A team that reaches for a workflow to run six independent calls writes a graph, a definition source and
capability names for something a list would do. A team that reaches for a job to model an order's lifecycle ends up
with a status field and `if` statements again. Pick by the shape of the problem, not by which block sounds most
powerful.

## The short version

| If your problem is... | Use | Because |
| --- | --- | --- |
| A body of work that must not be lost, made of steps that can run in parallel | [Job](jobs/index.md) | It plans steps up front, runs them on step grains, tracks progress and resumes after a restart |
| A business process that branches, waits for a person or a timer, or fans out over items | [Workflow](workflows/index.md) | The process is a versioned graph and every run is rebuilt from its own journal |
| One grain whose behavior depends on which state it is in | [State machine](state-machines/index.md) | States say which transitions are legal and what happens on entry and exit |
| A grain whose state follows Chronicle events | [Event-sourced state machine](state-machines/index.md) | The events stay the only source of truth, and the grain derives its state from a read model |
| One long-running piece of work that must not block a grain's turn | [Worker](workers.md) | `GrainWithBackgroundTask` runs the work off-turn and hands back its result |
| Request-scoped logic, a cache, or a simple entity with a little state | A plain Orleans grain | None of these blocks adds anything you need |

## Jobs

Choose a job when the question is "did all of this work get done?" A job's steps are grains of their own, so slow
steps run side by side, up to `JobsOptions.MaxParallelSteps` at a time. The job keeps its plan and progress in
storage, so you can show how far it got, stop it, retry the failed steps and let it resume after a restart. Provisioning
a tenant, rebuilding an index and importing a file are all jobs.

A job is the wrong fit when the next step depends on a decision, a person or a deadline. A job has stages, which run
in order, but no branches and no waiting for outside input. It is also the wrong fit when nobody needs to observe or
resume the work. A hosted service is lighter then.

## Workflows

Choose a workflow when the process itself is the thing you want to see, change and audit: wait for an approval, call a
capability, wait a day, fan out over the lines of an order and join the results. The definition is data, a run pins the
version it started with, and each run is rebuilt from a journal of facts, so you can answer what happened to order 1001
without reading logs. The node work runs as jobs, which gives it retries and recovery for free.

A workflow is the wrong fit when the steps are a straight list. You pay for a definition source, named capabilities and
a journal, and a job does the same job with none of that. It is also the wrong fit when the behavior belongs to one
entity and is driven by its own events, because a state machine is the natural model there.

## State machines

Choose a state machine when a grain has a lifecycle: a document that is drafted, published and archived, or an observer
that is catching up, observing or replaying. Each state is an object that names the states it can move to and what
runs on entry and exit, so no method has to remember which statuses it is valid in. The plain machine persists its
state with the grain. The event-sourced machine in `Cratis.Choreography.Chronicle` persists nothing: it derives its
state from a Chronicle read model every time it activates.

A state machine is the wrong fit when the status carries no behavior. An enum property is simpler then. It is also the
wrong fit when a transition runs long work, because entry and exit behavior runs on the grain's turn. Start the work
from the state and let it report back. The event-sourced variant is wrong when the grain must decide rather than
follow, and for work that has to happen exactly once per event, because Chronicle can redeliver an event.

## Workers

Choose a worker when you need a grain to run one piece of background work without blocking its turn, and to hand back
the result or the error afterwards. Job steps are built on this pattern, so you meet it through jobs most of the time.
Derive from `GrainWithBackgroundTask` directly when you want the same shape without a job around it.

A worker keeps no persisted state of its own. If the process dies, the work is gone. When it must survive, make it a
job step, which adds persisted state and recovery.

## Combining them

The blocks are layered, and you can use them together:

- A workflow node's work runs as a job, and a job step is a worker.
- A state machine can start a workflow when it enters a state. Call `StartWorkflow` from the state's `OnEnter`, a
  method in the `Cratis.Choreography.Chronicle` package, and the machine becomes the run's subject.

## Where to go next

- [Run your first durable job](getting-started/index.md) builds a job and resumes it after a kill.
- [Run your first workflow](getting-started/first-workflow.md) builds a three-node graph.
- [Hosting](hosting/index.md) covers the silo these blocks run in.
