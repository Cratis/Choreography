// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Runs;
using Cratis.Choreography.Workflows.Waits;

namespace Cratis.Choreography.Chronicle.Workflows.Listing;

/// <summary>
/// An open wait of a run.
/// </summary>
/// <param name="Wait">The wait.</param>
/// <param name="Execution">The execution that is waiting.</param>
public record WorkflowRunWait(WorkflowWaitId Wait, NodeExecutionKey Execution);
