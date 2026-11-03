// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Definitions.for_CompositeWorkflowDefinitionSource.when_getting_the_active_version.given;

public class sources : Specification
{
    protected static readonly WorkflowKey _workflow = new(new WorkflowScope("tenant"), "chain-1");
    protected static readonly WorkflowDefinitionSnapshot _snapshot = new("chain-1", "product", 1, "issue-chain", new WorkflowGraph([], []), "catalog", []);

    protected static IWorkflowDefinitions Source(bool owns)
    {
        var source = Substitute.For<IWorkflowDefinitions>();
        source.Owns(Arg.Any<WorkflowKey>()).Returns(owns);
        source.GetActive(Arg.Any<WorkflowKey>()).Returns(_snapshot);
        return source;
    }

    protected static CompositeWorkflowDefinitionSource Over(params IWorkflowDefinitions[] definitions) => new(new KnownInstancesOf(definitions));

    sealed class KnownInstancesOf(IEnumerable<IWorkflowDefinitions> instances) : IInstancesOf<IWorkflowDefinitions>
    {
        public IEnumerator<IWorkflowDefinitions> GetEnumerator() => instances.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
