// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.for_WorkflowGrain.when_admitting;

public class and_the_payload_is_not_json : given.a_workflow
{
    void Establish()
    {
        Activate();
        Trigger = Trigger with { Payload = "not JSON" };
    }

    async Task Because() => Admission = await Grain.Admit(Trigger);

    [Fact] void should_reject_the_trigger() => Admission.Admitted.ShouldBeFalse();
    [Fact] void should_explain_the_payload_rule() => Admission.Rejections.Single().Value.ShouldEqual("The trigger payload must be JSON of at most 32 KiB");
}
