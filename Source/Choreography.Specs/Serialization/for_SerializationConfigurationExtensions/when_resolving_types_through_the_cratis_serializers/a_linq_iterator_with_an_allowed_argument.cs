// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Serialization.for_SerializationConfigurationExtensions.when_resolving_types_through_the_cratis_serializers;

public class a_linq_iterator_with_an_allowed_argument : given.the_cratis_serializers
{
    bool _result;

    void Because() => _result = IsAcceptedByConverter(new[] { "first", "second" }.Select(_ => _.ToUpperInvariant()).GetType());

    [Fact] void should_be_accepted() => _result.ShouldBeTrue();
}
