// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs;

/// <summary>
/// How an attempt at a node failed.
/// </summary>
public enum WorkflowFailureKind
{
    /// <summary>The performer reported a failure, or threw.</summary>
    Error = 0,

    /// <summary>The attempt ran out of time.</summary>
    TimedOut = 1,

    /// <summary>The performer answered with an outlet the node does not declare.</summary>
    InvalidOutlet = 2,

    /// <summary>The output was not JSON, too large, or not the shape the node needs.</summary>
    InvalidOutput = 3,

    /// <summary>No performer is registered for the node's capability.</summary>
    PerformerMissing = 4,

    /// <summary>The node was reached more often than its policy allows.</summary>
    VisitLimitExceeded = 5
}
