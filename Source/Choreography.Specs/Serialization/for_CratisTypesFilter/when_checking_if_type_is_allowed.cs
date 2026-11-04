// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Serialization.for_CratisTypesFilter;

public class when_checking_if_type_is_allowed : Specification
{
    CratisTypesFilter _filter;
    bool? _cratisType;
    bool? _cratisArray;
    bool? _generatedType;
    bool? _generatedGenericDefinition;
    bool? _unrelatedType;

    void Establish() => _filter = new CratisTypesFilter();

    void Because()
    {
        _cratisType = _filter.IsTypeAllowed(typeof(CratisTypesFilter));
        _cratisArray = _filter.IsTypeAllowed(typeof(CratisTypesFilter[]));
        _generatedType = _filter.IsTypeAllowed(typeof(OrleansCodeGen.Cratis.Choreography.Serialization.for_CratisTypesFilter.Generated));
        _generatedGenericDefinition = _filter.IsTypeAllowed(typeof(OrleansCodeGen.Cratis.Choreography.Serialization.for_CratisTypesFilter.Generated<>));
        _unrelatedType = _filter.IsTypeAllowed(typeof(string));
    }

    [Fact] void should_allow_a_cratis_type() => _cratisType.ShouldEqual(true);
    [Fact] void should_allow_an_array_of_a_cratis_type() => _cratisArray.ShouldEqual(true);
    [Fact] void should_allow_a_generated_cratis_type() => _generatedType.ShouldEqual(true);
    [Fact] void should_allow_a_generated_cratis_generic_type_definition() => _generatedGenericDefinition.ShouldEqual(true);
    [Fact] void should_leave_an_unrelated_type_to_the_default_policy() => _unrelatedType.ShouldBeNull();
}
