// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Orleans.Workflows.Sweeping.for_WorkflowRunSweepService;

public class when_sweeping_once : Specification
{
    IWorkflowRunSweeper _sweeper = null!;
    DateTimeOffset _now;

    async Task Because()
    {
        _now = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
        var options = new WorkflowsOptions();
        options.Scopes.Add("one");
        options.Scopes.Add("two");
        var time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(_now);
        _sweeper = Substitute.For<IWorkflowRunSweeper>();
        var service = new WorkflowRunSweepService(new WorkflowScopes(Options.Create(options), []), _sweeper, Options.Create(options), time, NullLogger<WorkflowRunSweepService>.Instance);
        await service.SweepOnce(CancellationToken.None);
    }

    [Fact] void should_sweep_the_first_scope() => _sweeper.Received(1).SweepScope(new WorkflowScope("one"), _now);
    [Fact] void should_sweep_the_second_scope() => _sweeper.Received(1).SweepScope(new WorkflowScope("two"), _now);
}
