// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs.Stages;
using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Execution.for_WorkflowWaveJob.when_planning;

public class and_a_performer_is_staged : given.a_planning_job
{
    void Establish()
    {
        var staged = Substitute.For<IStagedWorkflowNodePerformer>();
        staged.Operations.Returns([new WorkflowOperationName("register"), new WorkflowOperationName("start")]);
        Performers.For(new CapabilityReference("spec.release", 1)).Returns(staged);
        Performers.For(new CapabilityReference("spec.notify", 1)).Returns(Substitute.For<IWorkflowNodePerformer>());
    }

    Task Because() => Plan(new("tenant/run-1", 1, [ItemFor("release/each:csharp/1", "spec.release"), ItemFor("notify//1", "spec.notify")]));

    IEnumerable<WorkflowWaveItem> Requests => Steps.Select(step => (WorkflowWaveItem)step.Request);

    [Fact] void should_plan_one_step_per_operation_and_one_for_the_whole_node() => Steps.Count.ShouldEqual(3);
    [Fact] void should_all_be_node_steps() => Steps.All(step => step.Type == typeof(IWorkflowNodeStep)).ShouldBeTrue();
    [Fact] void should_name_each_operation() => Requests.Where(item => item.Capability == "spec.release").Select(item => item.Operation).ShouldContainOnly("register", "start");
    [Fact] void should_put_the_first_operation_in_the_first_stage() => Steps.Single(step => ((WorkflowWaveItem)step.Request).Operation == "register").Stage.ShouldEqual(new JobStepStage(0));
    [Fact] void should_put_the_second_operation_behind_a_barrier() => Steps.Single(step => ((WorkflowWaveItem)step.Request).Operation == "start").Stage.ShouldEqual(new JobStepStage(WorkflowWaveJob.StageSpacing));
    [Fact] void should_perform_the_unstaged_node_whole() => Requests.Single(item => item.Capability == "spec.notify").Operation.ShouldEqual(string.Empty);
}
