// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Jobs;

/// <summary>
/// Defines how a job learns whether every silo that can host it reads a step plan kept outside the job document.
/// </summary>
public interface IJobStepPlanStorageSupport
{
    /// <summary>
    /// Gets whether every silo that hosts the job class supports step plans kept outside the job document.
    /// </summary>
    /// <param name="jobGrainType">The <see cref="GrainType"/> of the job.</param>
    /// <returns>True when every such silo advertises the support, false when any does not or it cannot be told.</returns>
    bool IsSupportedByEverySilo(GrainType jobGrainType);
}
