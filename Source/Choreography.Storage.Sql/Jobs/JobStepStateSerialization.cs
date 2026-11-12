// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Serialization;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Json;

namespace Cratis.Choreography.Storage.Sql.Jobs;

/// <summary>
/// Serializes the state of a job step to and from the JSON stored with it.
/// </summary>
/// <remarks>
/// The state is written as its runtime type, so the properties a derived step state adds are kept. It is
/// written with the host's options plus the converters needed for concepts, including concepts used as
/// dictionary keys. A host owns the converters for its own types, so a state needing one the host options do
/// not carry cannot be stored. Rows written before this contract used plain options (Pascal-cased, concepts as
/// objects); they are read with the same options and, when that fails, with plain options.
/// </remarks>
internal sealed class JobStepStateSerialization
{
    static readonly JsonSerializerOptions _legacyOptions = new();
    static readonly Lazy<JobStepStateSerialization> _default = new(() => new(CratisJsonSerializer.CreateSerializerOptions()));

    readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobStepStateSerialization"/> class.
    /// </summary>
    /// <param name="hostOptions">The host's <see cref="JsonSerializerOptions"/>; it is copied, never changed.</param>
    internal JobStepStateSerialization(JsonSerializerOptions hostOptions)
    {
        _options = new JsonSerializerOptions(hostOptions)
        {
            PropertyNameCaseInsensitive = true
        };
        AddIfMissing<ComplexKeyDictionaryJsonConverterFactory>(_options);
        AddIfMissing<EnumerableConceptAsJsonConverterFactory>(_options);
        AddIfMissing<ConceptAsJsonConverterFactory>(_options);
    }

    /// <summary>
    /// Gets the serialization built on the default Cratis options.
    /// </summary>
    internal static JobStepStateSerialization Default => _default.Value;

    /// <summary>
    /// Serializes a state as its runtime type.
    /// </summary>
    /// <param name="state">The state to serialize.</param>
    /// <param name="declaredType">The type to use when the state is null.</param>
    /// <returns>The JSON.</returns>
    internal string Serialize(object? state, Type declaredType) =>
        JsonSerializer.Serialize(state, state?.GetType() ?? declaredType, _options);

    /// <summary>
    /// Deserializes stored JSON, falling back to the plain options earlier rows were written with.
    /// </summary>
    /// <param name="json">The stored JSON.</param>
    /// <param name="type">The type to deserialize to.</param>
    /// <returns>The state, or null when the JSON holds null.</returns>
    internal object? Deserialize(string json, Type type)
    {
        try
        {
            return JsonSerializer.Deserialize(json, type, _options);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            try
            {
                return JsonSerializer.Deserialize(json, type, _legacyOptions);
            }
            catch (Exception fallbackException) when (fallbackException is JsonException or NotSupportedException or InvalidOperationException)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
                return null;
            }
        }
    }

    /// <summary>
    /// Converts an entity to a job step state, keeping the key properties consistent with the entity columns.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The state.</returns>
    internal JobStepState ToJobStepState(JobStep entity)
    {
        if (string.IsNullOrEmpty(entity.StateJson))
        {
            return new JobStepState
            {
                Id = new JobStepIdentifier((JobId)entity.JobId, (JobStepId)entity.JobStepId),
                Type = new JobStepType(entity.Type),
                Name = (JobStepName)entity.Name,
                Status = entity.Status,
                IsPrepared = entity.IsPrepared
            };
        }

        var jobStepState = (JobStepState?)Deserialize(entity.StateJson, typeof(JobStepState)) ?? new JobStepState();

        // Ensure the key properties are consistent with entity values
        jobStepState.Id = new JobStepIdentifier((JobId)entity.JobId, (JobStepId)entity.JobStepId);
        jobStepState.Type = new JobStepType(entity.Type);
        jobStepState.Name = (JobStepName)entity.Name;
        jobStepState.Status = entity.Status;
        jobStepState.IsPrepared = entity.IsPrepared;

        return jobStepState;
    }

    /// <summary>
    /// Converts a job step state to an entity, serializing it as its runtime type.
    /// </summary>
    /// <param name="jobStepState">The state.</param>
    /// <param name="entityId">Unique identifier for the entity.</param>
    /// <returns>The entity.</returns>
    internal JobStep ToEntity(JobStepState jobStepState, Guid entityId) =>
        new()
        {
            Id = entityId,
            JobId = jobStepState.Id.JobId.Value,
            JobStepId = jobStepState.Id.JobStepId.Value,
            Type = jobStepState.Type.Value,
            Name = jobStepState.Name.Value,
            Status = jobStepState.Status,
            IsPrepared = jobStepState.IsPrepared,
            StateJson = Serialize(jobStepState, jobStepState.GetType())
        };

    static void AddIfMissing<TConverter>(JsonSerializerOptions options)
        where TConverter : JsonConverter, new()
    {
        if (!options.Converters.OfType<TConverter>().Any())
        {
            options.Converters.Add(new TConverter());
        }
    }
}
