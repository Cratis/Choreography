// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Serialization.for_SerializationConfigurationExtensions.when_resolving_types_through_the_cratis_serializers;

public class a_cratis_generic_with_a_string_argument : given.the_cratis_serializers
{
    readonly given.CratisGeneric<string> _value = new("hello");
    bool _accepted;
    object _result;

    void Because()
    {
        _accepted = IsAcceptedByConverter(typeof(given.CratisGeneric<string>));
        _result = RoundTrip(_value);
    }

    [Fact] void should_be_accepted() => _accepted.ShouldBeTrue();
    [Fact] void should_round_trip_the_value() => _result.ShouldEqual(_value);
}
