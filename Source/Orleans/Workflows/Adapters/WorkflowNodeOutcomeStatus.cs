// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Adapters;

/// <summary>
/// How a performer's attempt at a node ended.
/// </summary>
public enum WorkflowNodeOutcomeStatus
{
    /// <summary>It did what was asked, and names the outlet to leave through.</summary>
    Succeeded = 0,

    /// <summary>It chose not to do anything - which is not a failure.</summary>
    Skipped = 1,

    /// <summary>It failed; the attempt may be retried.</summary>
    Failed = 2,

    /// <summary>Whether the effect happened is unknown - never retried automatically.</summary>
    Ambiguous = 3
}
