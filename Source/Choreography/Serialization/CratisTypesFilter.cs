// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Serialization;

namespace Cratis.Choreography.Serialization;

/// <summary>
/// Represents an <see cref="ITypeFilter"/> that allows all Cratis types - the same set that is
/// routed through the JSON serializer. Without it, polymorphic Cratis payloads (interface-typed
/// parameters and arrays of them, such as constraint definitions) are rejected by the Orleans
/// type manifest when a grain call crosses a silo boundary; same-silo calls only deep-copy and
/// never hit the type-name check, which is why this only surfaces in a cluster.
/// The grain call types Orleans generates for Cratis grain interfaces live under
/// <c language="csharp">OrleansCodeGen.Cratis</c>; since Orleans 10.4.1 a generic type definition is authorized on its own,
/// so a call to a generic grain method fails to serialize unless those are allowed too.
/// </summary>
public class CratisTypesFilter : ITypeFilter
{
    const string GeneratedCodePrefix = "OrleansCodeGen.Cratis";

    /// <inheritdoc/>
    public bool? IsTypeAllowed(Type type)
    {
        var elementType = type.IsArray ? type.GetElementType()! : type;
        var @namespace = elementType.Namespace;
        if (@namespace is null)
        {
            return null;
        }

        if (@namespace.StartsWith("Cratis", StringComparison.Ordinal) || @namespace.StartsWith(GeneratedCodePrefix, StringComparison.Ordinal))
        {
            return true;
        }

        return null;
    }
}
