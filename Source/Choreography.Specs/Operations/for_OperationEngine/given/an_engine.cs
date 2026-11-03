// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.InMemory;

namespace Cratis.Choreography.Operations.for_OperationEngine.given;

public class an_engine : Specification
{
    protected const string Scope = "model-1";
    protected const string Alice = "alice";
    protected const string Bob = "bob";

    protected a_host Host;
    protected OperationHistory History;
    protected OperationEngine Engine;

    void Establish()
    {
        Host = new();
        History = new(new OperationHistoryStorage(), TimeProvider.System);
        var definitions = new OperationDefinitions();

        definitions.Define<RenameModule>()
            .UndoWith(context => new RenameModule(context.Command.Module, context.GetMemento<string>()!))
            .CanUndoWhen(context => Host.Names[context.Command.Module] == context.Command.NewName, "The module was renamed again after this operation")
            .CanRedoWhen(context => Host.Names[context.Command.Module] == context.GetMemento<string>(), "The module was renamed by someone else since the undo");

        definitions.Define<ArchiveModules>()
            .UndoWith(context => new RestoreModules(context.Command.Modules));

        definitions.Define<Untouchable>();

        Engine = new(History, definitions, Host, new OperationLocks(), Substitute.For<IServiceProvider>());
    }

    protected Task<OperationOutcome> Rename(string actor, string module, string newName) => Engine.Execute(Scope, actor, new RenameModule(module, newName));
}
