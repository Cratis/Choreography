// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Choreography.Chronicle.Workflows.for_RegisterEventSourcedWorkflows;

public class when_registering : Specification
{
    ExplicitArtifacts _artifacts = null!;
    ServiceProvider _services = null!;
    List<(Type Type, string Id)> _registrations = [];

    void Establish()
    {
        _artifacts = new();
        _services = new ServiceCollection().BuildServiceProvider();
    }

    void Because()
    {
        RegisterEventSourcedWorkflows<ChronicleOptions>.Register(_artifacts, new EventSourcedWorkflowsOptions { ReadModels = false }, _services);

        // The snapshot and its registrations are internal to the client, so they are read by reflection.
        var snapshot = typeof(ExplicitArtifacts).GetMethod("CreateSnapshot", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!.Invoke(_artifacts, null)!;
        var registrations = (IEnumerable)snapshot.GetType().GetProperty("EventTypes")!.GetValue(snapshot)!;
        _registrations =
        [
            .. registrations.Cast<object>().Select(registration =>
            {
                var facet = registration.GetType().GetProperty("Id")!.GetValue(registration)!;
                var id = (EventTypeId)facet.GetType().GetProperty("Value")!.GetValue(facet)!;
                return ((Type)registration.GetType().GetProperty("Type")!.GetValue(registration)!, id.Value);
            })
        ];
    }

    void Destroy() => _services.Dispose();

    [Fact] void should_register_every_fact() => _registrations.Select(registration => registration.Type.Name).Order().ShouldEqual([.. WorkflowRunFacts.Types.Select(type => type.Name).Order()]);
    [Fact] void should_register_each_with_the_bare_type_name_as_identifier() => _registrations.Where(registration => registration.Id != registration.Type.Name).ShouldBeEmpty();
}
