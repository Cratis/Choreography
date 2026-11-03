// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using System.Text.Json;
using Cratis.Choreography.Chronicle.Workflows.Listing;
using Cratis.Chronicle.Projections;
using Cratis.Serialization;

namespace Cratis.Choreography.Chronicle.Workflows.for_explicitly_registered_facts;

/// <summary>
/// Builds the open waits projection with the client's own projection builder, which resolves the events it follows through
/// the event types the facts were explicitly registered as.
/// </summary>
public class when_building_the_open_waits_projection : given.the_client_event_types
{
    object _definition = null!;

    void Because()
    {
        var builder = new ProjectionBuilderFor<OpenWorkflowWait>("open-waits", typeof(OpenWorkflowWait), new CamelCaseNamingPolicy(), EventTypes, new JsonSerializerOptions());
        OpenWorkflowWait.Define(builder);
        _definition = typeof(ProjectionBuilderFor<OpenWorkflowWait>)
            .GetMethod("Build", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes)!.Invoke(builder, null)!;
    }

    /// <summary>
    /// Reads the event type identifiers a section of the definition is keyed by. The contracts are not part of the compile-time surface of the client, so the definition is read by reflection.
    /// </summary>
    /// <param name="property">The name of the section.</param>
    /// <returns>The identifiers.</returns>
    IEnumerable<string> EventTypeIdsOf(string property)
    {
        var dictionary = (IDictionary)_definition.GetType().GetProperty(property)!.GetValue(_definition)!;
        return dictionary.Keys.Cast<object>().Select(eventType => eventType.GetType().GetProperty("Id")!.GetValue(eventType)!.ToString()!);
    }

    [Fact] void should_add_a_wait_when_it_opens() => EventTypeIdsOf("From").ShouldContainOnly("WorkflowWaitOpened");
    [Fact] void should_remove_it_when_it_is_resolved_or_cancelled() => EventTypeIdsOf("RemovedWith").Order().ShouldEqual(["WorkflowWaitCancelled", "WorkflowWaitResolved"]);
}
