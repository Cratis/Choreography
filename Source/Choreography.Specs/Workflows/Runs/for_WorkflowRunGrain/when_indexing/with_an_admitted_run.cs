// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Choreography.Workflows.Runs.for_WorkflowRunGrain.when_indexing;

public class with_an_admitted_run : given.a_run
{
    Task Because() => Grain.Admit(Definition, Trigger);

    [Fact] void should_add_the_run_to_the_index_of_its_scope() => Index.Verify(index => index.Add(Key.Run), Times.Once());
    [Fact] void should_not_remove_it() => Index.Verify(index => index.Remove(It.IsAny<WorkflowRunId>()), Times.Never());
}
