// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Runs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Orleans.Workflows.Sweeping.for_WorkflowRunSweeper;

public class when_sweeping_a_scope : Specification
{
    static readonly DateTimeOffset _now = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    IWorkflowRun _good = null!;
    IWorkflowRun _broken = null!;
    IWorkflowRun _after = null!;

    async Task Because()
    {
        var index = Substitute.For<IWorkflowRunIndex>();
        index.GetActive().Returns(Task.FromResult<IReadOnlyList<WorkflowRunId>>([new WorkflowRunId("good"), new WorkflowRunId("broken"), new WorkflowRunId("after")]));
        _good = Substitute.For<IWorkflowRun>();
        _broken = Substitute.For<IWorkflowRun>();
        _broken.Tend(Arg.Any<DateTimeOffset>()).Returns<Task>(_ => throw new IOException("the run could not be reached"));
        _after = Substitute.For<IWorkflowRun>();
        var grains = Substitute.For<IGrainFactory>();
        grains.GetGrain<IWorkflowRunIndex>("scope-1").Returns(index);
        grains.GetGrain<IWorkflowRun>("scope-1/good").Returns(_good);
        grains.GetGrain<IWorkflowRun>("scope-1/broken").Returns(_broken);
        grains.GetGrain<IWorkflowRun>("scope-1/after").Returns(_after);
        await new WorkflowRunSweeper(grains, NullLogger<WorkflowRunSweeper>.Instance).SweepScope(new WorkflowScope("scope-1"), _now);
    }

    [Fact] void should_tend_every_run_the_index_holds() => _good.Received(1).Tend(_now);
    [Fact] void should_carry_on_after_a_run_that_could_not_be_tended() => _after.Received(1).Tend(_now);
}
