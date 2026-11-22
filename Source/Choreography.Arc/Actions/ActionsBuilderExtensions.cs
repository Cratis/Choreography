// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Actions;

namespace Cratis.Choreography.Arc.Actions;

/// <summary>
/// Extension methods that add Cratis Arc actions to an <see cref="ActionsBuilder"/>.
/// </summary>
public static class ActionsBuilderExtensions
{
    /// <summary>
    /// Execute a Cratis Arc command, populated by code.
    /// </summary>
    /// <typeparam name="TCommand">The type of command, created with its default constructor.</typeparam>
    /// <param name="builder">The <see cref="ActionsBuilder"/>.</param>
    /// <param name="populate">Populates the command.</param>
    /// <returns>The builder for continuation.</returns>
    public static ActionsBuilder Execute<TCommand>(this ActionsBuilder builder, Action<TCommand> populate)
        where TCommand : class, new() => builder.Execute<TCommand>((command, _) => populate(command));

    /// <summary>
    /// Execute a Cratis Arc command, populated by code that has the <see cref="ActionContext"/>.
    /// </summary>
    /// <typeparam name="TCommand">The type of command, created with its default constructor.</typeparam>
    /// <param name="builder">The <see cref="ActionsBuilder"/>.</param>
    /// <param name="populate">Populates the command.</param>
    /// <returns>The builder for continuation.</returns>
    public static ActionsBuilder Execute<TCommand>(this ActionsBuilder builder, Action<TCommand, ActionContext> populate)
        where TCommand : class, new() => builder.Add(new ExecuteCommandAction(context =>
        {
            var command = new TCommand();
            populate(command, context);
            return command;
        }));

    /// <summary>
    /// Execute a Cratis Arc command that the code creates, for a command that cannot be created with a default constructor.
    /// </summary>
    /// <typeparam name="TCommand">The type of command.</typeparam>
    /// <param name="builder">The <see cref="ActionsBuilder"/>.</param>
    /// <param name="create">Creates the command.</param>
    /// <returns>The builder for continuation.</returns>
    public static ActionsBuilder Execute<TCommand>(this ActionsBuilder builder, Func<ActionContext, TCommand> create)
        where TCommand : class => builder.Add(new ExecuteCommandAction(context => create(context)));
}
