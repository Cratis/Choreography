// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;
using Orleans.Serialization.TypeSystem;

namespace Cratis.Choreography.Serialization.for_CratisTypesFilter.given;

public class a_type_converter_with_the_filter : Specification
{
    protected CratisTypesFilter _filter;
    protected TypeConverter _converter;

    void Establish()
    {
        _filter = new CratisTypesFilter();
        var services = new ServiceCollection();
        services.AddSingleton<ITypeFilter>(_filter);
        services.AddSerializer();
        _converter = services.BuildServiceProvider().GetRequiredService<TypeConverter>();
    }

    /// <summary>
    /// Resolves a type from its name the way a received message does, reporting whether the converter accepted it.
    /// </summary>
    /// <param name="type">The type to resolve.</param>
    /// <returns><see langword="true"/> when the converter resolved and accepted the type.</returns>
    protected bool IsAcceptedByConverter(Type type)
    {
        try
        {
            return _converter.TryParse(RuntimeTypeNameFormatter.Format(type), out var parsed) && parsed == type;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
