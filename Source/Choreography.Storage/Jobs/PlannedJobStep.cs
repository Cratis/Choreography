// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Jobs.Stages;

namespace Cratis.Choreography.Storage.Jobs;

/// <summary>
/// A step's durable identity, stage and preparation input, saved before any step is prepared.
/// </summary>
/// <param name="Id">The identity to reuse during recovery.</param>
/// <param name="Type">The grain interface type.</param>
/// <param name="Stage">The stage in which the step runs.</param>
/// <param name="RequestType">The assembly-qualified CLR type of the preparation request.</param>
/// <param name="RequestJson">The serialized preparation request.</param>
public record PlannedJobStep(JobStepId Id, JobStepType Type, JobStepStage Stage, string RequestType, string RequestJson);
