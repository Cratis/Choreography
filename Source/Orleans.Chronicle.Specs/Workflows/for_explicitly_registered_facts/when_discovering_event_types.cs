// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Runs;

namespace Cratis.Orleans.Chronicle.Workflows.for_explicitly_registered_facts;

public class when_discovering_event_types : given.the_client_event_types
{
    [Fact] void should_know_every_fact_as_an_event_type() => WorkflowRunFacts.Types.All(EventTypes.HasFor).ShouldBeTrue();
    [Fact] void should_identify_each_by_its_bare_type_name() => WorkflowRunFacts.Types.Where(type => EventTypes.GetEventTypeFor(type).Id.Value != type.Name).ShouldBeEmpty();
    [Fact] void should_map_each_identifier_back_to_the_fact() => WorkflowRunFacts.Types.Where(type => EventTypes.GetClrTypeFor(type.Name) != type).ShouldBeEmpty();
    [Fact] void should_know_them_all_as_the_event_types_of_the_store() => EventTypes.AllClrTypes.Count(type => WorkflowRunFacts.Types.Contains(type)).ShouldEqual(WorkflowRunFacts.Types.Count);
}
