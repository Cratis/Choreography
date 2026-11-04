// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Serialization.for_SerializationConfigurationExtensions.when_resolving_types_through_the_cratis_serializers;

public class a_cratis_generic_with_a_collection_expression_of_a_foreign_argument : given.the_cratis_serializers
{
    bool _result;

    void Because()
    {
        IReadOnlyList<SomeHost.ForeignType> items = [new(), new()];
        _result = IsAcceptedByConverter(typeof(given.CratisGeneric<>).MakeGenericType(items.GetType()));
    }

    [Fact] void should_be_refused() => _result.ShouldBeFalse();
}
