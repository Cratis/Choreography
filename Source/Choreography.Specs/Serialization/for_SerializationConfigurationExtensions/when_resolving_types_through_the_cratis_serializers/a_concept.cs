// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Serialization.for_SerializationConfigurationExtensions.when_resolving_types_through_the_cratis_serializers;

public class a_concept : given.the_cratis_serializers
{
    readonly given.CratisConcept _value = new(Guid.Parse("4f1c3c5e-2a7b-4e0f-9d4e-8c2b1a6f3e10"));
    bool _accepted;
    object _result;

    void Because()
    {
        _accepted = IsAcceptedByConverter(typeof(given.CratisConcept));
        _result = RoundTrip(_value);
    }

    [Fact] void should_be_accepted() => _accepted.ShouldBeTrue();
    [Fact] void should_round_trip_the_value() => _result.ShouldEqual(_value);
}
