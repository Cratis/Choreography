// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs;

/// <summary>
/// A workflow run finished with a failure nothing handled.
/// </summary>
/// <param name="Reason">Why, as a <see cref="WorkflowFailureReason"/>.</param>
public record WorkflowRunFailed(WorkflowFailureReason Reason);
