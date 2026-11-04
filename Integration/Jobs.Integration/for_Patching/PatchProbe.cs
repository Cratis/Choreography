// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Jobs.Integration.for_Patching;

/// <summary>
/// Records which patches ran in the silo, in order.
/// </summary>
public class PatchProbe
{
    readonly List<string> _applied = [];

    /// <summary>
    /// Gets the names of the patches applied, in the order they ran.
    /// </summary>
    public IReadOnlyList<string> Applied
    {
        get
        {
            lock (_applied)
            {
                return [.. _applied];
            }
        }
    }

    /// <summary>
    /// Records that a patch ran.
    /// </summary>
    /// <param name="name">The name of the patch.</param>
    public void Record(string name)
    {
        lock (_applied)
        {
            _applied.Add(name);
        }
    }
}
