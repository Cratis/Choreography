// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations;

/// <summary>
/// Represents how an undo, redo or execute request ended.
/// </summary>
public enum OperationOutcomeKind
{
    /// <summary>
    /// The operation was performed.
    /// </summary>
    Succeeded = 0,

    /// <summary>
    /// The target operation cannot be undone or redone right now. The reason says why; no older operation was tried.
    /// </summary>
    Blocked = 1,

    /// <summary>
    /// The actor has no operation in the scope to undo.
    /// </summary>
    NothingToUndo = 2,

    /// <summary>
    /// The actor has no undone operation in the scope to redo.
    /// </summary>
    NothingToRedo = 3,

    /// <summary>
    /// The host failed to execute the command.
    /// </summary>
    Failed = 4
}
