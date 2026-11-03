// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Orleans.Storage.Jobs;

/// <summary>
/// Identifies one publication of a job's stored step plan.
/// </summary>
/// <param name="JobId">The job the plan belongs to.</param>
/// <param name="Incarnation">The fresh identity persisted before publishing the plan.</param>
public record JobStepPlanKey(JobId JobId, Guid Incarnation);
