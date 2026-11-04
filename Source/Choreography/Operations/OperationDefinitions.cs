// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Choreography.Operations;

/// <summary>
/// Represents the default <see cref="IOperationDefinitions"/>.
/// </summary>
public class OperationDefinitions : IOperationDefinitions
{
    readonly ConcurrentDictionary<OperationKind, IOperationDefinition> _byKind = new();

    /// <inheritdoc/>
    public OperationDefinition<TCommand> Define<TCommand>(OperationKind? kind = null)
        where TCommand : notnull
    {
        kind ??= new OperationKind(typeof(TCommand).FullName ?? typeof(TCommand).Name);
        var definition = new OperationDefinition<TCommand>(kind);
        if (!_byKind.TryAdd(kind, definition))
        {
            throw new OperationAlreadyDefined(kind);
        }

        return definition;
    }

    /// <inheritdoc/>
    public IOperationDefinition? Find(OperationKind kind) => _byKind.GetValueOrDefault(kind);

    /// <inheritdoc/>
    public IOperationDefinition? Find(Type commandType) => _byKind.Values.FirstOrDefault(_ => _.CommandType == commandType);
}
