// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations;

/// <summary>
/// Represents one operation in an actor's history as a client sees it. A plain record, ready to serialize.
/// </summary>
/// <param name="Id">The operation identity.</param>
/// <param name="Position">Position in the history; items are ordered by it.</param>
/// <param name="Scope">The scope.</param>
/// <param name="Actor">The actor.</param>
/// <param name="Kind">The kind of operation.</param>
/// <param name="Role">Forward, undo or redo.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="EventSequenceNumbers">References to the events the operation produced.</param>
/// <param name="Started">When it was recorded.</param>
/// <param name="Target">The operation an undo or redo applies to.</param>
/// <param name="CanUndo">True only for the operation an undo request would reverse, when it is safe to.</param>
/// <param name="CanRedo">True only for the operation a redo request would reissue, when it is eligible.</param>
/// <param name="BlockedReason">Why an undo or redo of this operation is currently blocked, when it is the next target.</param>
public record OperationHistoryItem(
    Guid Id,
    ulong Position,
    string Scope,
    string Actor,
    string Kind,
    OperationRole Role,
    OperationStatus Status,
    IReadOnlyList<ulong> EventSequenceNumbers,
    DateTimeOffset Started,
    Guid? Target,
    bool CanUndo,
    bool CanRedo,
    string? BlockedReason);
