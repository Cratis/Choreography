// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationEngine.given;

/// <summary>
/// A fake host owning the "current state" - module names - and appending one event per change.
/// </summary>
public class a_host : IOperationExecutor
{
    ulong _sequenceNumber;

    public Dictionary<string, string> Names { get; } = new() { ["a"] = "a", ["b"] = "b", ["c"] = "c" };

    public HashSet<string> Archived { get; } = [];

    public List<object> Commands { get; } = [];

    public bool Fail { get; set; }

    public Task<OperationExecutionResult> Execute(object command, OperationExecutionContext context)
    {
        Commands.Add(command);
        if (Fail)
        {
            return Task.FromResult(OperationExecutionResult.Failure("The host rejected the command"));
        }

        switch (command)
        {
            case RenameModule rename:
                var previous = Names[rename.Module];
                Names[rename.Module] = rename.NewName;
                return Task.FromResult(OperationExecutionResult.Success([++_sequenceNumber], previous));
            case ArchiveModules archive:
                archive.Modules.ToList().ForEach(_ => Archived.Add(_));
                return Task.FromResult(OperationExecutionResult.Success(archive.Modules.Select(_ => ++_sequenceNumber).ToArray()));
            case RestoreModules restore:
                restore.Modules.ToList().ForEach(_ => Archived.Remove(_));
                return Task.FromResult(OperationExecutionResult.Success(restore.Modules.Select(_ => ++_sequenceNumber).ToArray()));
            default:
                return Task.FromResult(OperationExecutionResult.Success([++_sequenceNumber]));
        }
    }
}
