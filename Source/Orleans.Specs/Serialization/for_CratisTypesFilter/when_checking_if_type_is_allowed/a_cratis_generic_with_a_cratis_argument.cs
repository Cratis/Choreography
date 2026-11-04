// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Serialization.for_CratisTypesFilter.when_checking_if_type_is_allowed;

public class a_cratis_generic_with_a_cratis_argument : given.a_type_converter_with_the_filter
{
    readonly Type _type = typeof(given.CratisGeneric<given.CratisType>);
    bool? _result;

    void Because() => _result = _filter.IsTypeAllowed(_type);

    [Fact] void should_leave_it_to_the_generic_arguments() => _result.ShouldBeNull();
    [Fact] void should_be_accepted_by_the_type_converter() => IsAcceptedByConverter(_type).ShouldBeTrue();
}
