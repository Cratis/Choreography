// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations;

/// <summary>
/// Represents the lifecycle status of an operation.
/// </summary>
public enum OperationStatus
{
    /// <summary>
    /// The operation has been recorded and is executing.
    /// </summary>
    Started = 0,

    /// <summary>
    /// The operation finished and is in effect.
    /// </summary>
    Completed = 1,

    /// <summary>
    /// The operation failed. Events associated before the failure are kept as references.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// The operation was reversed by an undo operation. It is a candidate for redo.
    /// </summary>
    Undone = 3,

    /// <summary>
    /// The operation was undone and later reissued by a redo operation.
    /// </summary>
    Redone = 4,

    /// <summary>
    /// The operation was undone but can no longer be redone, because its actor performed a new operation.
    /// </summary>
    Discarded = 5
}
