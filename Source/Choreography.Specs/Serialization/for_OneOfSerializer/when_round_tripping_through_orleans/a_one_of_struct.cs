// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OneOf;

namespace Cratis.Choreography.Serialization.for_OneOfSerializer.when_round_tripping_through_orleans;

public class a_one_of_struct : given.a_configured_orleans_serializer
{
    OneOf<string, int> _first;
    OneOf<string, int> _second;

    void Because()
    {
        _first = RoundTrip<OneOf<string, int>>("hello");
        _second = RoundTrip<OneOf<string, int>>(42);
    }

    [Fact] void should_keep_the_first_case_index() => _first.Index.ShouldEqual(0);
    [Fact] void should_keep_the_first_case_value() => _first.AsT0.ShouldEqual("hello");
    [Fact] void should_keep_the_second_case_index() => _second.Index.ShouldEqual(1);
    [Fact] void should_keep_the_second_case_value() => _second.AsT1.ShouldEqual(42);
}
