// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Triggers;

namespace Cratis.Choreography.Chronicle.Workflows.for_WorkflowStateExtensions;

public class when_starting_a_workflow_for_an_event_sourced_machine : given.a_state_in_a_machine
{
    void Because() => StateOf<given.EventSourcedMachineForTesting>("store-1/ns-1/order-42").StartWorkflow("workflow-1").GetAwaiter().GetResult();

    [Fact] void should_start_it_in_the_scope_of_the_namespace_of_the_machine() => StartedIn.ShouldEqual(new WorkflowScope("scope-of-ns-1"));
    [Fact] void should_make_the_machine_the_subject() => Started!.Subject.Id.Value.ShouldEqual("order-42");
    [Fact] void should_name_the_machine_type_as_the_subject_kind() => Started!.Subject.Kind.Value.ShouldEqual(nameof(given.EventSourcedMachineForTesting));
    [Fact] void should_correlate_with_the_subject() => Started!.Correlation.Value.ShouldEqual("order-42");
    [Fact] void should_start_as_a_stage_hook() => Started!.Kind.ShouldEqual(WorkflowTriggerKind.StageHook);
    [Fact] void should_not_start_again_for_the_same_machine_and_state() => Started!.Deduplication.Value.EndsWith(":store-1/ns-1/order-42", StringComparison.Ordinal).ShouldBeTrue();
}
