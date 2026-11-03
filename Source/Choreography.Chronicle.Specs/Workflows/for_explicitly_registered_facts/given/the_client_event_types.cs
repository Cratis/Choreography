// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Schemas;
using Cratis.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Choreography.Chronicle.Workflows.for_explicitly_registered_facts.given;

/// <summary>
/// The event types of an event store the way the Chronicle client builds them when the run facts are registered explicitly:
/// the same registration the adapter makes, applied to the client's artifact catalog, and a real <see cref="EventTypes"/> discovering from it.
/// </summary>
public class the_client_event_types : Specification
{
    protected EventTypes EventTypes = null!;

    async Task Establish()
    {
        var artifacts = new ExplicitArtifacts();
        await using var services = new ServiceCollection().BuildServiceProvider();
        RegisterEventSourcedWorkflows<ChronicleOptions>.Register(artifacts, new EventSourcedWorkflowsOptions(), services);

        // The snapshot and the catalog it builds are internal to the client, so they are reached by reflection.
        const BindingFlags all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var snapshot = typeof(ExplicitArtifacts).GetMethod("CreateSnapshot", all)!.Invoke(artifacts, null)!;
        var apply = snapshot.GetType().GetMethods(all).Single(method => method.Name == "ApplyTo" && method.GetParameters() is [{ ParameterType: var parameter }] && parameter == typeof(IClientArtifactsProvider));
        var catalog = (IClientArtifactsProvider)apply.Invoke(snapshot, [DefaultClientArtifactsProvider.Default])!;

        var eventStore = Substitute.For<IEventStore>();
        eventStore.Name.Returns(new EventStoreName("workflows"));
        var generator = new JsonSchemaGenerator(Substitute.For<IComplianceMetadataResolver>(), new CamelCaseNamingPolicy(), new DerivedTypes(new Cratis.Types.Types()));
        EventTypes = new EventTypes(eventStore, generator, catalog, Substitute.For<IEventTypeMigrators>(), false);
        await EventTypes.Discover();
    }
}
