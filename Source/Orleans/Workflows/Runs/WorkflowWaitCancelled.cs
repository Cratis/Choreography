// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Waits;

namespace Cratis.Orleans.Workflows.Runs;

/// <summary>
/// A wait was called off with its run.
/// </summary>
/// <param name="Wait">The <see cref="WorkflowWaitId"/>.</param>
public record WorkflowWaitCancelled(WorkflowWaitId Wait);
