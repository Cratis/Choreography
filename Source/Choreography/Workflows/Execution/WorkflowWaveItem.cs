// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Execution;

/// <summary>
/// One node execution attempt a wave job runs - or one operation of it, for a staged performer.
/// </summary>
/// <param name="Run">The <see cref="WorkflowRunKey"/> text of the run to report back to.</param>
/// <param name="Execution">The <see cref="Runs.NodeExecutionKey"/> text.</param>
/// <param name="Attempt">The attempt.</param>
/// <param name="Capability">The capability identity.</param>
/// <param name="CapabilityVersion">The capability version.</param>
/// <param name="Configuration">The node's configuration JSON.</param>
/// <param name="Input">The node's input JSON.</param>
/// <param name="IdempotencyKey">The key the performer deduplicates external effects on.</param>
/// <param name="TimeoutSeconds">How long the attempt may run.</param>
/// <param name="Operation">The operation of a staged performer this item performs - empty for the whole node.</param>
/// <param name="Evidence">The evidence the run's trigger carried.</param>
/// <remarks>
/// Primitives rather than the concepts they stand for, like every job request here - a person reading a
/// job's persisted state should not need to know the shape of a concept to see what was asked for.
/// </remarks>
public record WorkflowWaveItem(
    string Run,
    string Execution,
    int Attempt,
    string Capability,
    int CapabilityVersion,
    string Configuration,
    string Input,
    string IdempotencyKey,
    int TimeoutSeconds,
    string Operation,
    IEnumerable<string> Evidence);
