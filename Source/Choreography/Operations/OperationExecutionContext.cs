// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations;

/// <summary>
/// Describes the operation a command is being executed for, handed to the host's <see cref="IOperationExecutor"/>.
/// </summary>
/// <param name="OperationId">The operation the command belongs to. Hosts can stamp it on the events they append.</param>
/// <param name="Scope">The scope.</param>
/// <param name="Actor">The actor the command is executed for.</param>
/// <param name="Kind">The kind of operation.</param>
/// <param name="Role">Whether this is a forward, undo or redo execution.</param>
/// <param name="Target">The operation being undone or redone; null for a forward execution.</param>
public record OperationExecutionContext(
    OperationId OperationId,
    OperationScope Scope,
    OperationActor Actor,
    OperationKind Kind,
    OperationRole Role,
    OperationId? Target);
