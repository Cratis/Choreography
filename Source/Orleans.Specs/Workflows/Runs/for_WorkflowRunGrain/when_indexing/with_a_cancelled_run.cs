// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Workflows.Runs.for_WorkflowRunGrain.when_indexing;

public class with_a_cancelled_run : given.a_run
{
    async Task Because()
    {
        await Grain.Admit(Definition, Trigger);
        await Grain.Cancel("called off");
    }

    [Fact] void should_remove_the_run_once_it_is_over() => Index.Verify(index => index.Remove(Key.Run), Times.Once());
}
