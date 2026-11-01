// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Runs;

/// <summary>
/// A workflow run was called off.
/// </summary>
/// <param name="Reason">Why, as a <see cref="WorkflowFailureReason"/>.</param>
public record WorkflowRunCancelled(WorkflowFailureReason Reason);
