// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Definitions;
using Cratis.Orleans.Workflows.Triggers;

namespace Cratis.Orleans.Workflows.for_WorkflowGrain.when_admitting;

public class and_a_policy_rejects : given.a_workflow
{
    void Establish()
    {
        Activate();
        Policy.Check(Key, Arg.Any<WorkflowDefinitionSnapshot>(), Arg.Any<WorkflowTrigger>()).Returns([new WorkflowAdmissionRejection("not allowed here")]);
    }

    async Task Because() => Admission = await Grain.Admit(Trigger);

    [Fact] void should_reject_explicitly() => Admission.Admitted.ShouldBeFalse();
    [Fact] void should_carry_the_policy_answer() => Admission.Rejections.ShouldContainOnly(new WorkflowAdmissionRejection("not allowed here"));
}
