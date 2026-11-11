// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs.Coordination;

/// <summary>
/// Identifies the attempt of a node execution being reported.
/// </summary>
/// <param name="Execution">The node execution.</param>
/// <param name="Attempt">The attempt within that execution.</param>
internal readonly record struct NodeAttempt(NodeExecutionKey Execution, WorkflowAttempt Attempt);
