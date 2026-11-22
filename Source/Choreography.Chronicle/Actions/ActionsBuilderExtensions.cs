// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Actions;
using Cratis.Choreography.Chronicle.StateMachines;

namespace Cratis.Choreography.Chronicle.Actions;

/// <summary>
/// Extension methods that add Chronicle actions to an <see cref="ActionsBuilder"/>.
/// </summary>
public static class ActionsBuilderExtensions
{
    /// <summary>
    /// Append an event, populated by code.
    /// </summary>
    /// <typeparam name="TEvent">The type of event, created with its default constructor.</typeparam>
    /// <param name="builder">The <see cref="ActionsBuilder"/>.</param>
    /// <param name="populate">Populates the event.</param>
    /// <param name="target">Optional <see cref="StateMachineKey"/> to append to - see <see cref="AppendEventAction"/> for the default.</param>
    /// <returns>The builder for continuation.</returns>
    public static ActionsBuilder Append<TEvent>(this ActionsBuilder builder, Action<TEvent> populate, Func<ActionContext, StateMachineKey>? target = default)
        where TEvent : class, new() => builder.Append<TEvent>((@event, _) => populate(@event), target);

    /// <summary>
    /// Append an event, populated by code that has the <see cref="ActionContext"/>.
    /// </summary>
    /// <typeparam name="TEvent">The type of event, created with its default constructor.</typeparam>
    /// <param name="builder">The <see cref="ActionsBuilder"/>.</param>
    /// <param name="populate">Populates the event.</param>
    /// <param name="target">Optional <see cref="StateMachineKey"/> to append to - see <see cref="AppendEventAction"/> for the default.</param>
    /// <returns>The builder for continuation.</returns>
    public static ActionsBuilder Append<TEvent>(this ActionsBuilder builder, Action<TEvent, ActionContext> populate, Func<ActionContext, StateMachineKey>? target = default)
        where TEvent : class, new() => builder.Add(new AppendEventAction(
            context =>
            {
                var @event = new TEvent();
                populate(@event, context);
                return @event;
            },
            target));

    /// <summary>
    /// Append an event that the code creates, for an event that cannot be created with a default constructor, such as a record.
    /// </summary>
    /// <typeparam name="TEvent">The type of event.</typeparam>
    /// <param name="builder">The <see cref="ActionsBuilder"/>.</param>
    /// <param name="create">Creates the event.</param>
    /// <param name="target">Optional <see cref="StateMachineKey"/> to append to - see <see cref="AppendEventAction"/> for the default.</param>
    /// <returns>The builder for continuation.</returns>
    public static ActionsBuilder Append<TEvent>(this ActionsBuilder builder, Func<ActionContext, TEvent> create, Func<ActionContext, StateMachineKey>? target = default)
        where TEvent : class => builder.Add(new AppendEventAction(context => create(context), target));
}
