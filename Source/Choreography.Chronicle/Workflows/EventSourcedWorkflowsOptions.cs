// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Triggers;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Choreography.Chronicle.Workflows;

/// <summary>
/// Represents the options of the event-sourced workflow engine.
/// </summary>
public class EventSourcedWorkflowsOptions
{
    readonly List<WorkflowEventTrigger> _eventTriggers = [];
    readonly List<WorkflowSchedule> _schedules = [];

    /// <summary>
    /// Gets or sets the event store the runs' facts are appended to. Defaults to <c lang="csharp">Default</c>.
    /// </summary>
    public EventStoreName EventStore { get; set; } = new("Default");

    /// <summary>
    /// Gets or sets how a <see cref="WorkflowScope"/> maps onto the event store namespace its runs live in. Defaults to the scope's value.
    /// </summary>
    public Func<WorkflowScope, EventStoreNamespaceName> NamespaceFor { get; set; } = scope => new(scope.Value);

    /// <summary>
    /// Gets or sets how an event store namespace maps back onto a <see cref="WorkflowScope"/>. Defaults to the namespace's value.
    /// </summary>
    public Func<EventStoreNamespaceName, WorkflowScope> ScopeFor { get; set; } = @namespace => new(@namespace.Value);

    /// <summary>
    /// Gets or sets whether the run listing and the open waits are maintained as read models. Defaults to true.
    /// </summary>
    public bool ReadModels { get; set; } = true;

    /// <summary>
    /// Gets the event triggers that have been declared.
    /// </summary>
    public IReadOnlyList<WorkflowEventTrigger> EventTriggers => _eventTriggers;

    /// <summary>
    /// Gets the schedules that have been declared.
    /// </summary>
    public IReadOnlyList<WorkflowSchedule> Schedules => _schedules;

    /// <summary>
    /// Declares that an event admits a run of a workflow.
    /// </summary>
    /// <typeparam name="TEvent">The type of event.</typeparam>
    /// <param name="workflow">The <see cref="WorkflowId"/> of the workflow to admit a run of.</param>
    /// <param name="trigger">Builds the trigger from the event, or answers null when the event should not start a run.</param>
    /// <returns>The same options for continuation.</returns>
    /// <remarks>
    /// The trigger's deduplication key decides whether the event starts a run again: the same key is the same run, so
    /// an event delivered twice - a replay, a recovered partition - finds the run the first delivery started. The event's
    /// namespace becomes the scope of the run, through <see cref="ScopeFor"/>.
    /// </remarks>
    public EventSourcedWorkflowsOptions OnEvent<TEvent>(WorkflowId workflow, Func<TEvent, EventContext, WorkflowTrigger?> trigger)
        where TEvent : class
    {
        _eventTriggers.Add(new(
            new ReactorId($"Cratis.Choreography.Workflows.OnEvent.{typeof(TEvent).Name}.{workflow.Value}"),
            workflow,
            typeof(TEvent),
            (builder, start) => builder.On<TEvent>((@event, context) => start(trigger(@event, context), context))));
        return this;
    }

    /// <summary>
    /// Declares that a workflow starts on a schedule.
    /// </summary>
    /// <param name="schedule">The <see cref="WorkflowSchedule"/>.</param>
    /// <returns>The same options for continuation.</returns>
    public EventSourcedWorkflowsOptions Schedule(WorkflowSchedule schedule)
    {
        _schedules.Add(schedule);
        return this;
    }
}
