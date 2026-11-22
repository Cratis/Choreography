// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Actions;

/// <summary>
/// Identifies a grain - what kind of grain it is, its Orleans grain type and its key.
/// </summary>
/// <param name="Kind">The <see cref="GrainKind"/>.</param>
/// <param name="Type">The Orleans grain type name.</param>
/// <param name="Key">The grain key, as text.</param>
public record GrainIdentity(GrainKind Kind, string Type, string Key)
{
    /// <summary>
    /// Create the <see cref="GrainIdentity"/> of a grain from its Orleans grain id.
    /// </summary>
    /// <param name="grain">The grain to identify.</param>
    /// <param name="kind">The <see cref="GrainKind"/> of the grain.</param>
    /// <returns>The <see cref="GrainIdentity"/>.</returns>
    public static GrainIdentity Of(Grain grain, GrainKind kind)
    {
        var id = grain.GetGrainId();
        return new(kind, id.Type.ToString(), id.Key.ToString());
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Kind}:{Type}/{Key}";
}
