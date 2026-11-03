// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Waits;

/// <summary>
/// What a wait is waiting for.
/// </summary>
public enum WaitKind
{
    /// <summary>A signal from outside, matched on correlation and contract.</summary>
    Signal = 0,

    /// <summary>Time to pass.</summary>
    Timer = 1,

    /// <summary>A person to approve or reject.</summary>
    Approval = 2
}
