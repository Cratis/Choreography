// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Orleans.Workflows.Execution;

/// <summary>
/// What a <see cref="WorkflowWaveJob"/> was asked to run.
/// </summary>
/// <param name="Run">The <see cref="WorkflowRunKey"/> text of the run.</param>
/// <param name="Wave">The wave number within the run.</param>
/// <param name="Items">The <see cref="WorkflowWaveItem">items</see> to run.</param>
public record WorkflowWaveRequest(string Run, int Wave, IEnumerable<WorkflowWaveItem> Items) : IJobRequest;
