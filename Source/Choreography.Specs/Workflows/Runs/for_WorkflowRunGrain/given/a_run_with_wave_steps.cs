// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Execution;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Scopes;
using Cratis.Monads;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Orleans.TestKit;

namespace Cratis.Choreography.Workflows.Runs.for_WorkflowRunGrain.given;

/// <summary>
/// Connects the real run, wave planning and node-step implementations with simulated Orleans contexts.
/// This is boundary coverage, not a live silo test.
/// </summary>
public class a_run_with_wave_steps : a_run
{
    static readonly MethodInfo _prepare = typeof(WorkflowWaveJob).GetMethod("PrepareSteps", BindingFlags.Instance | BindingFlags.NonPublic)!;
    static readonly MethodInfo _perform = typeof(WorkflowNodeStep).GetMethod("PerformStep", BindingFlags.Instance | BindingFlags.NonPublic)!;

    protected readonly Queue<WorkflowWaveRequest> Waves = [];
    protected IWorkflowNodePerformer Performer = null!;
    protected IStagedWorkflowNodePerformer Staged = null!;
    protected IWorkflowNodePerformers Performers = null!;
    protected List<int> Stages = [];
    protected List<WorkflowNodeInvocation> Invocations = [];
    IServiceScopeFactory _scopes = null!;

    void Establish()
    {
        Performer = Substitute.For<IWorkflowNodePerformer>();
        Performer.Perform(Arg.Any<WorkflowNodeInvocation>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            Invocations.Add(call.Arg<WorkflowNodeInvocation>());
            return WorkflowNodeOutcome.Succeeded("next", "{\"performed\":true}");
        });
        Staged = Substitute.For<IStagedWorkflowNodePerformer>();
        Staged.Operations.Returns([new WorkflowOperationName("register"), new WorkflowOperationName("start")]);
        Staged.PerformOperation(Arg.Any<WorkflowNodeInvocation>(), Arg.Any<WorkflowOperationName>(), Arg.Any<Contracts.WorkflowData>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            Invocations.Add(call.Arg<WorkflowNodeInvocation>());
            return WorkflowNodeOutcome.Succeeded("next", "{\"stageCompleted\":true}");
        });
        Performers = Substitute.For<IWorkflowNodePerformers>();
        Performers.For(Arg.Any<CapabilityReference>()).Returns(Performer);
        Performers.For(Arg.Is<CapabilityReference>(capability => capability.Id.Value == "staged")).Returns(Staged);
        var grains = Substitute.For<IGrainFactory>();
        grains.GetGrain<IWorkflowRun>(Arg.Any<string>(), Arg.Any<string?>()).Returns(_ => Grain);
        var services = new ServiceCollection().AddSingleton(Performers).AddSingleton(grains).AddSingleton(Substitute.For<IWorkflowNodeExecutionContext>()).BuildServiceProvider();
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(services);
        _scopes = Substitute.For<IServiceScopeFactory>();
        _scopes.CreateAsyncScope().Returns(new AsyncServiceScope(scope));
        RecordWaves();
    }

    protected void RecordWaves() =>
        Jobs.Setup(jobs => jobs.Start<IWorkflowWaveJob, WorkflowWaveRequest>(It.IsAny<JobId>(), It.IsAny<WorkflowWaveRequest>()))
            .ReturnsAsync((JobId id, WorkflowWaveRequest request) =>
            {
                Waves.Enqueue(request);
                return Result<JobId, StartJobError>.Success(id);
            });

    protected async Task DrainWaves()
    {
        var count = 0;
        while (Waves.TryDequeue(out var request))
        {
            if (++count > 10)
            {
                throw new IOException("The runtime specification exceeded its ten-wave fuse");
            }

            var silo = new TestKitSilo();
            silo.AddService(_scopes);
            silo.AddService(Scopes);
            var jobTypes = Substitute.For<IJobTypes>();
            jobTypes.GetFor(Arg.Any<Type>()).Returns(Result<JobType, IJobTypes.GetForError>.Success(new JobType("WorkflowWave")));
            silo.AddService(jobTypes);
            var job = await silo.CreateGrainAsync<WorkflowWaveJob>(JobId.New(), new JobKey("workflows", Key.Scope.Value));
            var steps = await (Task<IImmutableList<JobStepDetails>>)_prepare.Invoke(job, [request])!;
            foreach (var details in steps.OrderBy(step => step.Stage.Value))
            {
                Stages.Add(details.Stage.Value);
                await PerformStep((WorkflowWaveItem)details.Request);
            }
        }
    }

    protected async Task PerformStep(WorkflowWaveItem item)
    {
        var silo = new TestKitSilo();
        silo.AddService(_scopes);
        silo.AddService(Scopes);
        silo.AddService(Substitute.For<IJobStepThrottle>());
        var step = await silo.CreateGrainAsync<WorkflowNodeStep>(JobStepId.New(), new JobStepKey(JobId.New(), "workflows", Key.Scope.Value));
        await (Task<Cratis.Monads.Catch<JobStepResult>>)_perform.Invoke(step, [new WorkflowNodeStepState { Item = item }, CancellationToken.None])!;
    }
}
