// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Contracts;

namespace Cratis.Choreography.Workflows.for_WorkflowGrain.when_admitting;

public class and_the_subject_does_not_match : given.a_workflow
{
    void Establish()
    {
        Activate();
        Trigger = Trigger with { Subject = new WorkflowSubjectReference("another-kind", "subject") };
    }

    async Task Because() => Admission = await Grain.Admit(Trigger);

    [Fact] void should_reject_the_trigger() => Admission.Admitted.ShouldBeFalse();
    [Fact] void should_explain_the_mismatch() => Admission.Rejections.Single().Value.ShouldEqual("The active workflow or subject does not match the trigger");
}
