// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CS0618 // The forwarder is obsolete on purpose; the spec proves it still delegates.

namespace Cratis.Choreography.Availability.for_forwarder_types;

public class when_inspecting_the_forwarder_types : Specification
{
    static readonly Type[] _forwarders =
    [
        typeof(Cratis.Choreography.Availability.AvailabilityOptions),
        typeof(Cratis.Choreography.Availability.AvailabilityCatalog),
        typeof(Cratis.Choreography.Availability.AvailabilityEvaluator),
        typeof(Cratis.Choreography.Availability.InMemoryAvailabilityManifest),
        typeof(Cratis.Choreography.Availability.CircularAvailabilityException),
        typeof(Cratis.Choreography.Availability.UndefinedAvailabilityException),
        typeof(Cratis.Choreography.Availability.EnvironmentSwitchRefusedException),
        typeof(Cratis.Choreography.Availability.Audiences.AudienceResolver),
        typeof(Cratis.Choreography.Availability.Audiences.DeclaredAudiences),
        typeof(Cratis.Choreography.Availability.Audiences.InMemoryAudienceSource)
    ];

    [Fact] void should_derive_every_forwarder_from_the_arc_type_of_the_same_name() =>
        _forwarders.All(_ => _.BaseType!.Name == _.Name && _.BaseType.Namespace!.StartsWith("Cratis.Arc.Availability")).ShouldBeTrue();

    [Fact] void should_mark_every_forwarder_obsolete() =>
        _forwarders.All(_ => Attribute.IsDefined(_, typeof(ObsoleteAttribute), false)).ShouldBeTrue();

    [Fact] void should_point_every_obsolete_message_to_arc() =>
        _forwarders.All(_ => ((ObsoleteAttribute)Attribute.GetCustomAttribute(_, typeof(ObsoleteAttribute), false)!).Message.Contains("Cratis.Arc.Availability")).ShouldBeTrue();
}
