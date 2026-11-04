// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Serialization.for_CratisTypesFilter.when_checking_if_type_is_allowed;

public class a_generated_cratis_generic_type_definition : given.a_type_converter_with_the_filter
{
    bool? _result;

    void Because() => _result = _filter.IsTypeAllowed(typeof(OrleansCodeGen.Cratis.Choreography.Serialization.for_CratisTypesFilter.Generated<>));

    [Fact] void should_allow_it() => _result.ShouldEqual(true);
}
