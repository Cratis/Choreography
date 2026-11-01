// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Contracts;

namespace Cratis.Orleans.Workflows.Runs;

/// <summary>
/// Pins the immutable input of a logical execution, shared by all attempts and recovered waves.
/// </summary>
/// <param name="Execution">The logical execution.</param>
/// <param name="Input">The bounded invocation input.</param>
public record WorkflowNodeInputPinned(NodeExecutionKey Execution, WorkflowData Input);
