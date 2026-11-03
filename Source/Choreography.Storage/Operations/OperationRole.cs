// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations;

/// <summary>
/// Represents the role an operation plays in an actor's history.
/// </summary>
public enum OperationRole
{
    /// <summary>
    /// An ordinary operation performed by the actor.
    /// </summary>
    Forward = 0,

    /// <summary>
    /// An operation that reverses an earlier operation. It targets that operation.
    /// </summary>
    Undo = 1,

    /// <summary>
    /// An operation that reissues the intent of an undone operation. It targets that operation.
    /// </summary>
    Redo = 2
}
