// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Definitions;

namespace Cratis.Choreography.Workflows.for_WorkflowGrain.when_admitting;

public class and_no_version_is_active : given.a_workflow
{
    async Task Because() => Admission = await Grain.Admit(Trigger);

    [Fact] void should_reject_explicitly() => Admission.Admitted.ShouldBeFalse();
    [Fact] void should_name_the_missing_version() => Admission.Rejections.ShouldContainOnly(WorkflowAdmissionRejection.NoActiveVersion);
    [Fact] void should_resolve_the_definition_from_the_tenant_qualified_key() => Source.Received(1).GetActive(Key);
}
