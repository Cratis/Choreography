// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OneOf;

namespace Cratis.Choreography.Serialization.for_OneOfSerializer.when_round_tripping_through_orleans;

public class a_one_of_base_derivative : given.a_configured_orleans_serializer
{
    TextOrNumber _result;

    void Because() => _result = RoundTrip(new TextOrNumber(7));

    [Fact] void should_round_trip_to_the_same_derived_type() => _result.ShouldBeOfExactType<TextOrNumber>();
    [Fact] void should_keep_the_case_index() => _result.Index.ShouldEqual(1);
    [Fact] void should_keep_the_case_value() => _result.AsT1.ShouldEqual(7);

    public class TextOrNumber(OneOf<string, int> input) : OneOfBase<string, int>(input);
}
