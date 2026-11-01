// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Contracts;

namespace Cratis.Orleans.Workflows.Runs;

/// <summary>
/// One branch a for-each fanned out - the item it runs for.
/// </summary>
/// <param name="Key">The <see cref="WorkflowBranchKey"/>.</param>
/// <param name="Item">The item, as <see cref="WorkflowData"/>.</param>
public record WorkflowBranch(WorkflowBranchKey Key, WorkflowData Item);
