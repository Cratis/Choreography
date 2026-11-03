// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Orleans.Storage;

namespace Cratis.Choreography.Workflows.Journal.for_GrainStorageWorkflowRunJournal.given;

/// <summary>
/// A grain storage that honors ETags the way the real providers do, standing in for the provider named <c lang="csharp">workflows</c>.
/// </summary>
public class a_storage : IGrainStorage
{
    readonly Dictionary<GrainId, (object State, string ETag)> _stored = [];

    public int Writes { get; private set; }

    public bool LoseTheNextWrite { get; set; }

    public Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        if (_stored.TryGetValue(grainId, out var stored))
        {
            grainState.State = (T)Clone(stored.State);
            grainState.ETag = stored.ETag;
            grainState.RecordExists = true;
        }

        return Task.CompletedTask;
    }

    public Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        Writes++;
        var current = _stored.TryGetValue(grainId, out var stored) ? stored.ETag : null;
        if (LoseTheNextWrite || current != grainState.ETag)
        {
            LoseTheNextWrite = false;
            throw new InconsistentStateException("the state was written by someone else", current ?? string.Empty, grainState.ETag ?? string.Empty);
        }

        var etag = Guid.NewGuid().ToString();
        _stored[grainId] = (Clone(grainState.State!), etag);
        grainState.ETag = etag;
        grainState.RecordExists = true;
        return Task.CompletedTask;
    }

    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        _stored.Remove(grainId);
        return Task.CompletedTask;
    }

    public GrainStorageWorkflowRunJournal Journal() =>
        new(new ServiceCollection().AddKeyedSingleton<IGrainStorage>(WellKnownGrainStorageProviders.Workflows, this).BuildServiceProvider());

    static object Clone(object state) => System.Text.Json.JsonSerializer.Deserialize(System.Text.Json.JsonSerializer.Serialize(state), state.GetType())!;
}
