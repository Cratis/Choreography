// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;
using Orleans.Serialization.TypeSystem;

namespace Cratis.Choreography.Serialization.for_SerializationConfigurationExtensions.given;

public class the_cratis_serializers : Specification
{
    protected TypeConverter _converter;
    protected Serializer _serializer;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddCratisChoreographySerializers();
        var provider = services.BuildServiceProvider();
        _converter = provider.GetRequiredService<TypeConverter>();
        _serializer = provider.GetRequiredService<Serializer>();
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

    /// <summary>
    /// Round-trips a value typed as <see cref="object"/>, so its type name travels with it and is resolved on read.
    /// </summary>
    /// <param name="value">The value to round-trip.</param>
    /// <returns>The value read back.</returns>
    protected object RoundTrip(object value) => _serializer.Deserialize<object>(_serializer.SerializeToArray(value))!;
}
