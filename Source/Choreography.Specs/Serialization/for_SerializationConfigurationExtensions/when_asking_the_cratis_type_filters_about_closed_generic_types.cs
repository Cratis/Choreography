// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Microsoft.Extensions.DependencyInjection;
using OneOf;
using Orleans.Serialization;

namespace Cratis.Choreography.Serialization.for_SerializationConfigurationExtensions;

/// <summary>
/// Guards the rule every Cratis type filter follows: a closed generic type is never allowed outright, because
/// Orleans would then skip checking its generic arguments. A new filter that breaks the rule fails here.
/// </summary>
public class when_asking_the_cratis_type_filters_about_closed_generic_types : Specification
{
    static readonly Type[] _closedGenericTypes =
    [
        typeof(given.CratisGeneric<SomeHost.ForeignType>),
        typeof(given.CratisGeneric<SomeHost.ForeignType>[]),
        typeof(ConceptAs<SomeHost.ForeignComparable>),
        typeof(OneOf<SomeHost.ForeignType, string>),
        typeof(Result<SomeHost.ForeignType, string>),
        new SomeHost.ForeignType[] { new() }.Select(_ => "item").GetType(),
        ((IReadOnlyList<SomeHost.ForeignType>)[new(), new()]).GetType(),
    ];

    ITypeFilter[] _filters;
    string[] _allowed;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddCratisChoreographySerializers();
        _filters = services.BuildServiceProvider()
            .GetServices<ITypeFilter>()
            .Where(_ => _.GetType().Namespace?.StartsWith("Cratis", StringComparison.Ordinal) == true)
            .ToArray();
    }

    void Because() => _allowed =
    [
        .. from filter in _filters
           from type in _closedGenericTypes
           where filter.IsTypeAllowed(type) == true
           select $"{filter.GetType().Name} allows {type}"
    ];

    [Fact] void should_find_the_cratis_filters() => _filters.Length.ShouldBeGreaterThan(3);
    [Fact] void should_not_allow_any_closed_generic_type() => _allowed.ShouldBeEmpty();
}
