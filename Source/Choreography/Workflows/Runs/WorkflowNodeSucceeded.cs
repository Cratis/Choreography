// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs;

/// <summary>
/// A node execution succeeded and left through an outlet.
/// </summary>
/// <param name="Execution">The <see cref="NodeExecutionKey"/>.</param>
/// <param name="Attempt">The <see cref="WorkflowAttempt"/> that succeeded.</param>
/// <param name="Outlet">The <see cref="OutletName"/> it left through.</param>
/// <param name="Output">What it produced, as <see cref="WorkflowData"/>.</param>
/// <param name="Evidence">The evidence it recorded.</param>
public record WorkflowNodeSucceeded(NodeExecutionKey Execution, WorkflowAttempt Attempt, OutletName Outlet, WorkflowData Output, IEnumerable<EvidenceReference> Evidence);
