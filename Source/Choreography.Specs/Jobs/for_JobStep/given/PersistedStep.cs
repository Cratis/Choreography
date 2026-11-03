// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.Jobs;

namespace Cratis.Choreography.Jobs.for_JobStep.given;

/// <summary>
/// What a step write carried to storage.
/// </summary>
/// <param name="Status">The persisted status.</param>
/// <param name="Message">The persisted progress message.</param>
/// <param name="Attempt">The persisted attempt.</param>
/// <param name="Lease">The persisted lease, if any.</param>
public record PersistedStep(JobStepStatus Status, string Message, int Attempt = 1, JobStepLease? Lease = null);
