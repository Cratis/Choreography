// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Serialization;

namespace Cratis.Choreography.Serialization;

/// <summary>
/// Extension methods for recognizing closed generic types in <see cref="ITypeFilter"/> verdicts.
/// </summary>
/// <remarks>
/// Orleans treats an allowing <see cref="ITypeFilter"/> verdict on a closed generic type as final and does
/// not go on to check its generic arguments, so allowing one would also allow whatever its arguments are.
/// The filters therefore allow only generic type definitions and leave closed generic types undecided;
/// Orleans then checks every generic argument on its own and then the definition.
/// </remarks>
static class ClosedGenericTypes
{
    /// <summary>
    /// Whether a type is a closed generic type, directly or as the element type of an array.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><see langword="true"/> when the type, or its innermost element type, is a closed generic type.</returns>
    public static bool IsClosedGeneric(this Type type)
    {
        var elementType = type;
        while (elementType.HasElementType)
        {
            elementType = elementType.GetElementType()!;
        }

        return elementType.IsConstructedGenericType;
    }
}
