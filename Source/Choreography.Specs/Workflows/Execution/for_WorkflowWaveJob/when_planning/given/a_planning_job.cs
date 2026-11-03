// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Workflows.Adapters;
using Cratis.Monads;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestKit;

namespace Cratis.Choreography.Workflows.Execution.for_WorkflowWaveJob.when_planning.given;

/// <summary>
/// A wave job whose performer registry the spec fills.
/// </summary>
public class a_planning_job : Specification
{
    static readonly MethodInfo _prepareSteps = typeof(WorkflowWaveJob).GetMethod("PrepareSteps", BindingFlags.NonPublic | BindingFlags.Instance)!;

    protected TestKitSilo Silo = new();
    protected IWorkflowNodePerformers Performers = null!;
    protected WorkflowWaveJob Job = null!;
    protected IImmutableList<JobStepDetails> Steps = null!;

    async Task Establish()
    {
        Performers = Substitute.For<IWorkflowNodePerformers>();
        var serviceProvider = new ServiceCollection().AddSingleton(Performers).BuildServiceProvider();
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateAsyncScope().Returns(new AsyncServiceScope(scope));
        Silo.AddService(scopeFactory);

        var jobTypes = Substitute.For<IJobTypes>();
        jobTypes.GetFor(Arg.Any<Type>()).Returns(Result<JobType, IJobTypes.GetForError>.Success(new JobType("WorkflowWave")));
        Silo.AddService(jobTypes);

        Job = await Silo.CreateGrainAsync<WorkflowWaveJob>(JobId.New(), new JobKey("event-store", "namespace"));
    }

    protected static WorkflowWaveItem ItemFor(string execution, string capability) =>
        new("tenant/run-1", execution, 1, capability, 1, "{}", "{}", "idempotency", 60, WorkflowOperationName.None.Value, []);

    protected async Task Plan(WorkflowWaveRequest request) =>
        Steps = await (Task<IImmutableList<JobStepDetails>>)_prepareSteps.Invoke(Job, [request])!;
}
