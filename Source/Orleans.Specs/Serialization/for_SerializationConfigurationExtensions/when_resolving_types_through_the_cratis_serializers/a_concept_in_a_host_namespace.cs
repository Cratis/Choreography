// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Serialization.for_SerializationConfigurationExtensions.when_resolving_types_through_the_cratis_serializers;

public class a_concept_in_a_host_namespace : given.the_cratis_serializers
{
    readonly SomeHost.HostConcept _value = new(Guid.Parse("8d0f6a4e-3b1c-4f7a-9e2d-5c6b7a8f9e01"));
    bool _accepted;
    object _result;

    void Because()
    {
        _accepted = IsAcceptedByConverter(typeof(SomeHost.HostConcept));
        _result = RoundTrip(_value);
    }

    [Fact] void should_be_accepted() => _accepted.ShouldBeTrue();
    [Fact] void should_round_trip_the_value() => _result.ShouldEqual(_value);
}
