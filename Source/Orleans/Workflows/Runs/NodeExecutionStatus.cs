// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Runs;

/// <summary>
/// Where one node execution stands.
/// </summary>
public enum NodeExecutionStatus
{
    /// <summary>Activated, but nothing has happened yet.</summary>
    Pending = 0,

    /// <summary>Scheduled into a wave job; an outcome is expected.</summary>
    Scheduled = 1,

    /// <summary>Waiting for a signal, a timer or a person.</summary>
    Waiting = 2,

    /// <summary>Done, and left through an outlet.</summary>
    Succeeded = 3,

    /// <summary>Failed for good.</summary>
    Failed = 4,

    /// <summary>The performer chose not to do anything - distinct from failing.</summary>
    Skipped = 5,

    /// <summary>Never reached, because nothing led to it.</summary>
    Unreachable = 6,

    /// <summary>Called off with the run.</summary>
    Cancelled = 7,

    /// <summary>The outcome is unknown, and a person has to find out what happened. Never retried automatically.</summary>
    ReconciliationRequired = 8,

    /// <summary>Failed an attempt, and waiting for its retry.</summary>
    RetryPending = 9
}
