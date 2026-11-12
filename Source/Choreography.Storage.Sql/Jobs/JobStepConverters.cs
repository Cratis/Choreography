// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Choreography.Storage.Jobs;

namespace Cratis.Choreography.Storage.Sql.Jobs;

/// <summary>
/// Converters for job steps.
/// </summary>
public static class JobStepConverters
{
    /// <summary>
    /// Convert from entity to job step state.
    /// </summary>
    /// <param name="entity">Entity to convert from.</param>
    /// <returns>Converted job step state.</returns>
    public static JobStepState ToJobStepState(this JobStep entity) =>
        JobStepStateSerialization.Default.ToJobStepState(entity);

    /// <summary>
    /// Convert from entity to job step state.
    /// </summary>
    /// <param name="entity">Entity to convert from.</param>
    /// <param name="options">The host <see cref="JsonSerializerOptions"/> the state was written with.</param>
    /// <returns>Converted job step state.</returns>
    public static JobStepState ToJobStepState(this JobStep entity, JsonSerializerOptions options) =>
        new JobStepStateSerialization(options).ToJobStepState(entity);

    /// <summary>
    /// Convert from job step state to entity.
    /// </summary>
    /// <param name="jobStepState">Job step state to convert from.</param>
    /// <param name="entityId">Unique identifier for the entity.</param>
    /// <returns>Converted entity.</returns>
    public static JobStep ToEntity(this JobStepState jobStepState, Guid entityId) =>
        JobStepStateSerialization.Default.ToEntity(jobStepState, entityId);

    /// <summary>
    /// Convert from job step state to entity.
    /// </summary>
    /// <param name="jobStepState">Job step state to convert from.</param>
    /// <param name="entityId">Unique identifier for the entity.</param>
    /// <param name="options">The host <see cref="JsonSerializerOptions"/> to write the state with.</param>
    /// <returns>Converted entity.</returns>
    public static JobStep ToEntity(this JobStepState jobStepState, Guid entityId, JsonSerializerOptions options) =>
        new JobStepStateSerialization(options).ToEntity(jobStepState, entityId);
}
