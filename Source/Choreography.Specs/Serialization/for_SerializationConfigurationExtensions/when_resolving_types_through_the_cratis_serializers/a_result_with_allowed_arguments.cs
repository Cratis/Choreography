// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Choreography.Serialization.for_SerializationConfigurationExtensions.when_resolving_types_through_the_cratis_serializers;

public class a_result_with_allowed_arguments : given.the_cratis_serializers
{
    readonly Result<string, given.CratisType> _value = "hello";
    bool _accepted;
    object _result;

    void Because()
    {
        _accepted = IsAcceptedByConverter(typeof(Result<string, given.CratisType>));
        _result = RoundTrip(_value);
    }

    [Fact] void should_be_accepted() => _accepted.ShouldBeTrue();
    [Fact] void should_round_trip_the_value() => _result.ShouldEqual(_value);
}
