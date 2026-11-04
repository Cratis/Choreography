// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations;

/// <summary>
/// Represents one entry in the append-only operation history: a logical operation and the events it produced.
/// </summary>
/// <param name="Id">The stable identity of the operation.</param>
/// <param name="Position">The position in the history, assigned by storage. Entries are ordered by it.</param>
/// <param name="Scope">The scope the operation belongs to.</param>
/// <param name="Actor">The actor that performed the operation.</param>
/// <param name="Kind">The kind of operation.</param>
/// <param name="Role">The role of the operation in the actor's history.</param>
/// <param name="Status">The lifecycle status.</param>
/// <param name="EventSequenceNumbers">
/// The sequence numbers of the events the operation produced, zero or more. They are references only - they are
/// not proof that reversing the operation is safe.
/// </param>
/// <param name="Started">When the operation was recorded.</param>
/// <param name="Target">The operation an undo or redo operation applies to; null for a forward operation.</param>
/// <param name="Intent">The serialized intent (command) of the operation, if any.</param>
/// <param name="Memento">Serialized state the host captured while executing, needed to reverse it later.</param>
/// <param name="Failure">The reason the operation failed, if it did.</param>
public record OperationEntry(
    OperationId Id,
    ulong Position,
    OperationScope Scope,
    OperationActor Actor,
    OperationKind Kind,
    OperationRole Role,
    OperationStatus Status,
    IReadOnlyList<ulong> EventSequenceNumbers,
    DateTimeOffset Started,
    OperationId? Target,
    string? Intent,
    string? Memento,
    string? Failure);
