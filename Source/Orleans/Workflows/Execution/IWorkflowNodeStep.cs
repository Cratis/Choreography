// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Orleans.Workflows.Execution;

/// <summary>
/// Defines the grain for <see cref="WorkflowNodeStep"/>.
/// </summary>
public interface IWorkflowNodeStep : IJobStep<WorkflowWaveItem, object, WorkflowNodeStepState>;
