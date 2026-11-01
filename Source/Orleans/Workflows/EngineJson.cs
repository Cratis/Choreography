// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Json;

namespace Cratis.Orleans.Workflows;

/// <summary>
/// The JSON settings the engine reads node configuration and writes workflow data with.
/// </summary>
static class EngineJson
{
    /// <summary>
    /// Gets the shared <see cref="JsonSerializerOptions"/> - camel case, string enums, concepts as their values.
    /// </summary>
    public static readonly JsonSerializerOptions Options = Create();

    /// <summary>
    /// Reads a value from JSON text, answering null rather than throwing when the text does not fit.
    /// </summary>
    /// <typeparam name="T">The type to read.</typeparam>
    /// <param name="json">The JSON text.</param>
    /// <returns>The value, or null.</returns>
    public static T? TryRead<T>(string json)
        where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.Converters.Add(new ConceptAsJsonConverterFactory());
        return options;
    }
}
