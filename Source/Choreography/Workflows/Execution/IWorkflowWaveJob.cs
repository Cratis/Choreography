// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;

namespace Cratis.Choreography.Workflows.Execution;

/// <summary>
/// Defines the grain for <see cref="WorkflowWaveJob"/>.
/// </summary>
public interface IWorkflowWaveJob : IJob<WorkflowWaveRequest>;
