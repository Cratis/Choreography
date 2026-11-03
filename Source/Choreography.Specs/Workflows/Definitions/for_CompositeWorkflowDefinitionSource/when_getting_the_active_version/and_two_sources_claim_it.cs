// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Definitions.for_CompositeWorkflowDefinitionSource.when_getting_the_active_version;

public class and_two_sources_claim_it : given.sources
{
    Exception? _thrown;

    async Task Because() => _thrown = await Cratis.Specifications.Catch.Exception(() => Over(Source(owns: true), Source(owns: true)).GetActive(_workflow));

    [Fact] void should_refuse() => _thrown.ShouldBeOfExactType<MoreThanOneWorkflowDefinitionSource>();
}
