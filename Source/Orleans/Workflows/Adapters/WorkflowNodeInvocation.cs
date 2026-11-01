// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Contracts;
using Cratis.Orleans.Workflows.Graphs;
using Cratis.Orleans.Workflows.Runs;

namespace Cratis.Orleans.Workflows.Adapters;

/// <summary>
/// What a performer is asked to do.
/// </summary>
/// <param name="Run">The <see cref="WorkflowRunKey"/>.</param>
/// <param name="Execution">The <see cref="NodeExecutionKey"/>.</param>
/// <param name="Attempt">The <see cref="WorkflowAttempt"/>.</param>
/// <param name="IdempotencyKey">The <see cref="WorkflowIdempotencyKey"/> to deduplicate external effects on - the same for every attempt.</param>
/// <param name="Configuration">The node's <see cref="WorkflowNodeConfiguration"/>.</param>
/// <param name="Input">The node's input, as <see cref="WorkflowData"/>.</param>
/// <param name="Evidence">The evidence the run's trigger carried.</param>
public record WorkflowNodeInvocation(
    WorkflowRunKey Run,
    NodeExecutionKey Execution,
    WorkflowAttempt Attempt,
    WorkflowIdempotencyKey IdempotencyKey,
    WorkflowNodeConfiguration Configuration,
    WorkflowData Input,
    IEnumerable<EvidenceReference> Evidence);
