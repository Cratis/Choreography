// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Chronicle.Integration.Workflows;
using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Definitions;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Choreography.Workflows.Triggers;

namespace Cratis.Choreography.Chronicle.Integration.for_event_sourced_workflows;

/// <summary>
/// A run waits on a timer. The silo that holds some of the runs goes away; the runs it held are rebuilt from the facts in the
/// event log on the other silo, and act on the deadline when it is due.
/// </summary>
public class when_a_silo_goes_away_mid_run : Specification
{
    const int Runs = 16;

    static readonly WorkflowScope _scope = new("tenant-1");

    WorkflowClusterFixture _fixture = null!;
    List<WorkflowRunKey> _runs = [];
    int _heldBySecondary;
    List<WorkflowRunSnapshot> _afterTheSiloWentAway = [];
    List<WorkflowRunSnapshot> _afterTheDeadline = [];

    async Task Establish()
    {
        _fixture = new();
        _runs = [.. Enumerable.Range(0, Runs).Select(index => new WorkflowRunKey(_scope, new WorkflowRunId($"run-{index}-{Guid.NewGuid():N}")))];
        foreach (var key in _runs)
        {
            await Run(key).Admit(Definition(), Trigger(key));
        }

        var management = _fixture.GrainFactory.GetGrain<IManagementGrain>(0);
        var secondary = _fixture.Cluster.SecondarySilos[0].SiloAddress;
        _heldBySecondary = (await management.GetDetailedGrainStatistics())
            .Count(statistic => statistic.SiloAddress.Equals(secondary) && statistic.GrainType.Contains("WorkflowRunGrain", StringComparison.Ordinal));
    }

    async Task Because()
    {
        await _fixture.Cluster.StopSiloAsync(_fixture.Cluster.SecondarySilos[0]);
        foreach (var key in _runs)
        {
            _afterTheSiloWentAway.Add(await Run(key).GetSnapshot());
        }

        foreach (var key in _runs)
        {
            await Run(key).Tend(DateTimeOffset.UtcNow.AddHours(2));
            _afterTheDeadline.Add(await Run(key).GetSnapshot());
        }
    }

    void Destroy() => _fixture.Dispose();

    [Fact] void should_have_had_runs_on_the_silo_that_went_away() => _heldBySecondary.ShouldBeGreaterThan(0);
    [Fact] void should_still_know_every_run_was_waiting() => _afterTheSiloWentAway.TrueForAll(snapshot => snapshot.Status == WorkflowRunStatus.Waiting).ShouldBeTrue();
    [Fact] void should_act_on_the_deadline_of_every_run() => _afterTheDeadline.TrueForAll(snapshot => snapshot.Status == WorkflowRunStatus.Succeeded).ShouldBeTrue();
    [Fact] void should_have_admitted_each_run_once() => _runs.TrueForAll(key => InMemoryEventLog.Instance.FactsOf(key.Run.Value).OfType<WorkflowRunAdmitted>().Count() == 1).ShouldBeTrue();

    static WorkflowDefinitionSnapshot Definition() => new(
        "workflow-1",
        "area-1",
        1,
        "manual",
        new WorkflowGraph(
            [
                new WorkflowNode("trigger", WorkflowNodeKind.Trigger, new("manual", 1), WorkflowNodeConfiguration.Empty, ["next"], WorkflowNodePolicy.Default),
                new WorkflowNode("timer", WorkflowNodeKind.Wait, new("workflows.wait.timer", 1), "{\"kind\":\"timer\",\"after\":\"01:00:00\"}", ["elapsed"], WorkflowNodePolicy.Default)
            ],
            [new WorkflowEdge("trigger", "next", "timer")]),
        "catalog-1",
        []);

    static WorkflowTrigger Trigger(WorkflowRunKey key) => new(
        WorkflowTriggerKind.Manual,
        "integration",
        key.Run.Value,
        new("manual", "subject-1"),
        WorkflowContractReference.Any,
        WorkflowData.Empty,
        [],
        "correlation-1",
        WorkflowCausationKey.None,
        WorkflowActor.System,
        DateTimeOffset.UtcNow);

    IWorkflowRun Run(WorkflowRunKey key) => _fixture.GrainFactory.GetGrain<IWorkflowRun>(key.ToString());
}
