// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Definitions.for_CompositeWorkflowDefinitionSource.when_getting_the_active_version;

/// <summary>
/// An unowned workflow has no active version - its admission is rejected, never silently run.
/// </summary>
public class and_none_does : given.sources
{
    WorkflowDefinitionSnapshot? _active;

    async Task Because() => _active = await Over(Source(owns: false)).GetActive(_workflow);

    [Fact] void should_have_no_active_version() => _active.ShouldBeNull();
}
