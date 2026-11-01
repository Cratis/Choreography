// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Definitions.for_CompositeWorkflowDefinitionSource.when_getting_the_active_version;

public class and_a_source_owns_it : given.sources
{
    WorkflowDefinitionSnapshot? _active;

    async Task Because() => _active = await Over(Source(owns: false), Source(owns: true)).GetActive(_workflow);

    [Fact] void should_answer_with_the_owners_version() => _active.ShouldEqual(_snapshot);
}
