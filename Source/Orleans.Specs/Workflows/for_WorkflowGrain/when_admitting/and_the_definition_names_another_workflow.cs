// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Definitions;
using Cratis.Orleans.Workflows.Graphs;

namespace Cratis.Orleans.Workflows.for_WorkflowGrain.when_admitting;

public class and_the_definition_names_another_workflow : given.a_workflow
{
    void Establish() => Source.GetActive(Key).Returns(new WorkflowDefinitionSnapshot(WorkflowId.New(), "stored-area", 1, "spec", new WorkflowGraph([], []), "catalog", []));

    async Task Because() => Admission = await Grain.Admit(Trigger);

    [Fact] void should_reject_the_trigger() => Admission.Admitted.ShouldBeFalse();
    [Fact] void should_explain_the_mismatch() => Admission.Rejections.Single().Value.ShouldEqual("The active workflow or subject does not match the trigger");
}
