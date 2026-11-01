// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Orleans.Workflows.Runs;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Storage;

namespace Cratis.Orleans.Workflows.Journal;

/// <summary>
/// Represents an <see cref="IWorkflowRunJournal"/> that keeps a run's facts in Orleans grain storage - the provider
/// registered as <see cref="WellKnownGrainStorageProviders.Workflows"/>.
/// </summary>
/// <remarks>
/// A run's facts are one grain state, written with its ETag: an append reads the state, checks that it still
/// ends where the caller expected and writes it back, and a write that lost to another one is a conflict. The
/// whole journal is one document, so this journal suits runs of moderate length; a long-lived run with very
/// many facts belongs on an event-sourced journal.
/// </remarks>
/// <param name="services">The <see cref="IServiceProvider"/> the storage provider is resolved from.</param>
public class GrainStorageWorkflowRunJournal(IServiceProvider services) : IWorkflowRunJournal
{
    const string StateName = "WorkflowRunJournal";

    IGrainStorage Storage => services.GetRequiredKeyedService<IGrainStorage>(WellKnownGrainStorageProviders.Workflows);

    /// <inheritdoc/>
    public async Task<WorkflowRunJournalSnapshot> Load(WorkflowRunKey key)
    {
        var (_, journal) = await Read(key);
        return journal.Facts.Count == 0 ? WorkflowRunJournalSnapshot.Empty : Snapshot(journal);
    }

    /// <inheritdoc/>
    public async Task<WorkflowJournalAppendResult> Append(WorkflowRunKey key, WorkflowJournalPosition expectedTail, IReadOnlyList<object> facts)
    {
        var (state, journal) = await Read(key);
        var tail = TailOf(journal);
        if (tail != expectedTail)
        {
            return WorkflowJournalAppendResult.Conflict(expectedTail);
        }

        var now = DateTimeOffset.UtcNow.UtcTicks;
        foreach (var fact in facts)
        {
            journal.Facts.Add(new()
            {
                Type = WorkflowRunFacts.NameOf(fact.GetType()),
                Content = JsonSerializer.Serialize(fact, fact.GetType(), EngineJson.Options),
                OccurredTicks = now
            });
        }

        try
        {
            await Storage.WriteStateAsync(StateName, GrainIdFor(key), state);
        }
        catch (InconsistentStateException)
        {
            return WorkflowJournalAppendResult.Conflict(expectedTail);
        }

        return WorkflowJournalAppendResult.Appended(TailOf(journal));
    }

    static GrainId GrainIdFor(WorkflowRunKey key) => GrainId.Create("cratis-orleans-workflow-run-journal", key.ToString());

    static WorkflowJournalPosition TailOf(WorkflowRunJournalState state) => new(state.Facts.Count - 1);

    static WorkflowRunJournalSnapshot Snapshot(WorkflowRunJournalState state) => new(
        [.. state.Facts.Select(stored =>
        {
            var type = WorkflowRunFacts.TypeNamed(stored.Type) ?? throw new UnknownWorkflowFact(stored.Type);
            var fact = JsonSerializer.Deserialize(stored.Content, type, EngineJson.Options)
                ?? throw new UnknownWorkflowFact(stored.Type);
            return new WorkflowRecordedFact(fact, new DateTimeOffset(stored.OccurredTicks, TimeSpan.Zero));
        })],
        TailOf(state));

    async Task<(GrainState<WorkflowRunJournalState> State, WorkflowRunJournalState Journal)> Read(WorkflowRunKey key)
    {
        var state = new GrainState<WorkflowRunJournalState>(new());
        await Storage.ReadStateAsync(StateName, GrainIdFor(key), state);
        state.State ??= new();
        return (state, state.State);
    }
}
