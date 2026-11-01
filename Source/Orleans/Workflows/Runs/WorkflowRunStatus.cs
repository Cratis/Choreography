// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Runs;

/// <summary>
/// Where a workflow run stands.
/// </summary>
public enum WorkflowRunStatus
{
    /// <summary>Work is scheduled or about to be.</summary>
    Running = 0,

    /// <summary>Nothing runs; the run waits for a signal, a timer or a person.</summary>
    Waiting = 1,

    /// <summary>Finished, and nothing failed without being handled.</summary>
    Succeeded = 2,

    /// <summary>Finished, and something failed that nothing handled.</summary>
    Failed = 3,

    /// <summary>Called off.</summary>
    Cancelled = 4,

    /// <summary>Finished, but some outcome is unknown and a person has to find out what happened.</summary>
    ReconciliationRequired = 5
}
