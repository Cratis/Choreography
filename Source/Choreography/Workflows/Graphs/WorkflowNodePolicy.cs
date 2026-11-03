// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Graphs;

/// <summary>
/// How the engine runs a node - attempts, timeouts and how often it may be visited.
/// </summary>
/// <param name="MaxAttempts">How many attempts a failing node gets before it fails.</param>
/// <param name="AttemptTimeout">How long one attempt may run.</param>
/// <param name="RetryDelay">How long to wait before the next attempt.</param>
/// <param name="MaxVisits">How many times the node may run within one branch - more than one only on a cycle.</param>
public record WorkflowNodePolicy(int MaxAttempts, TimeSpan AttemptTimeout, TimeSpan RetryDelay, int MaxVisits)
{
    /// <summary>
    /// The policy of a node that says nothing else - one attempt of at most ten minutes, visited once.
    /// </summary>
    public static readonly WorkflowNodePolicy Default = new(1, TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(30), 1);
}
