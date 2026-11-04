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
/// </summary>
/// <remarks>
/// A constructed generic type is never allowed here, only its generic type definition. Orleans
/// treats an allowing filter verdict on a constructed generic type as final and stops at that
/// point, so allowing one would also allow whatever its generic arguments are. Leaving the
/// verdict open makes Orleans check every generic argument on its own and then the definition,
/// which this filter allows when it is a Cratis type.
/// </remarks>
public class CratisTypesFilter : ITypeFilter
{
    /// <inheritdoc/>
    public bool? IsTypeAllowed(Type type)
    {
        var elementType = type;
        while (elementType.HasElementType)
        {
            elementType = elementType.GetElementType()!;
        }

        if (!elementType.IsConstructedGenericType && (elementType.Namespace?.StartsWith("Cratis", StringComparison.Ordinal) ?? false))
        {
            return true;
        }

        return null;
    }
}
