// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows;

namespace Cratis.Orleans.Chronicle.Workflows.for_WorkflowStateExtensions;

public class when_starting_a_workflow_for_a_plain_machine : given.a_state_in_a_machine
{
    void Because() => StateOf<given.PlainMachineForTesting>("machine-1").StartWorkflow("workflow-1", deduplication: "once-per-entry-1", scope: new WorkflowScope("given-scope")).GetAwaiter().GetResult();

    [Fact] void should_start_it_in_the_given_scope() => StartedIn.ShouldEqual(new WorkflowScope("given-scope"));
    [Fact] void should_make_the_key_of_the_machine_the_subject() => Started!.Subject.Id.Value.ShouldEqual("machine-1");
    [Fact] void should_use_the_given_deduplication_key() => Started!.Deduplication.Value.ShouldEqual("once-per-entry-1");
}
