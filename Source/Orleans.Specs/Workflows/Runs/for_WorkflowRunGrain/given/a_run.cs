// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Workflows.Contracts;
using Cratis.Orleans.Workflows.Definitions;
using Cratis.Orleans.Workflows.Execution;
using Cratis.Orleans.Workflows.Graphs;
using Cratis.Orleans.Workflows.Journal;
using Cratis.Orleans.Workflows.Scopes;
using Cratis.Orleans.Workflows.Sweeping;
using Cratis.Orleans.Workflows.Triggers;
using Microsoft.Extensions.Options;
using Moq;
using Orleans.TestKit;

namespace Cratis.Orleans.Workflows.Runs.for_WorkflowRunGrain.given;

public class a_run : Specification
{
    protected TestKitSilo Silo = new();
    protected WorkflowRunGrain Grain = null!;
    protected a_journal Journal = new();
    protected Mock<IJobsManager> Jobs = null!;
    protected Mock<IWorkflowRunIndex> Index = null!;
    protected IWorkflowExecutionScopes Scopes = new DefaultWorkflowExecutionScopes(Options.Create(new WorkflowsOptions()));
    protected WorkflowRunKey Key = new(new WorkflowScope("tenant"), Guid.NewGuid().ToString());
    protected List<WorkflowRecordedFact> History => Journal.Facts;

    protected int Reads => Journal.Reads;

    protected int Appends => Journal.Appends;

    protected bool ConflictOnFirstAppend
    {
        get => Journal.ConflictOnFirstAppend;
        set => Journal.ConflictOnFirstAppend = value;
    }

    protected WorkflowDefinitionSnapshot Definition = new(
        WorkflowId.New(),
        "spec",
        1,
        "manual",
        new WorkflowGraph(
            [new WorkflowNode("trigger", WorkflowNodeKind.Trigger, new("manual", 1), WorkflowNodeConfiguration.Empty, ["next"], WorkflowNodePolicy.Default),
             new WorkflowNode("action", WorkflowNodeKind.Action, new("action", 1), WorkflowNodeConfiguration.Empty, ["next"], WorkflowNodePolicy.Default)],
            [new WorkflowEdge("trigger", "next", "action")]),
        "catalog",
        []);

    protected WorkflowTrigger Trigger = new(WorkflowTriggerKind.Manual, "spec", "one", new("manual", "subject"), WorkflowContractReference.Any, WorkflowData.Empty, [], "correlation", WorkflowCausationKey.None, WorkflowActor.System, DateTimeOffset.UtcNow);

    async Task Establish()
    {
        Jobs = Silo.AddProbe<IJobsManager>(0, Scopes.JobsManagerFor(Key.Scope).ToString());
        Jobs.Setup(jobs => jobs.GetJobsOfType<IWorkflowWaveJob, WorkflowWaveRequest>()).ReturnsAsync(ImmutableList<Cratis.Orleans.Storage.Jobs.JobState>.Empty);
        Jobs.Setup(jobs => jobs.Start<IWorkflowWaveJob, WorkflowWaveRequest>(It.IsAny<JobId>(), It.IsAny<WorkflowWaveRequest>()))
            .ReturnsAsync((JobId id, WorkflowWaveRequest _) => Result<JobId, StartJobError>.Success(id));
        Index = Silo.AddProbe<IWorkflowRunIndex>(Key.Scope.Value);
        AddServices(Silo);
        Grain = await Silo.CreateGrainAsync<WorkflowRunGrain>(Key.ToString());
    }

    protected async Task Reactivate()
    {
        Silo = new TestKitSilo();
        Silo.AddProbe(_ => Jobs.Object);
        Silo.AddProbe(_ => Index.Object);
        AddServices(Silo);
        Grain = await Silo.CreateGrainAsync<WorkflowRunGrain>(Key.ToString());
    }

    void AddServices(TestKitSilo silo)
    {
        silo.AddService<IWorkflowRunJournal>(Journal);
        silo.AddService(Scopes);
        silo.AddService(new WorkflowScopes(Options.Create(new WorkflowsOptions()), []));
        silo.AddService(Options.Create(new WorkflowsOptions { WakeRunsItself = false }));
        silo.AddService(TimeProvider.System);
    }
}
