// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Graphs;

/// <summary>
/// When a join is satisfied.
/// </summary>
public enum JoinMode
{
    /// <summary>Every branch succeeded.</summary>
    All = 0,

    /// <summary>At least one branch succeeded.</summary>
    Any = 1,

    /// <summary>At least <see cref="JoinConfiguration.Threshold"/> branches succeeded.</summary>
    AtLeast = 2
}
