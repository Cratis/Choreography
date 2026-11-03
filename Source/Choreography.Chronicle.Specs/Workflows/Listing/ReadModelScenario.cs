// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Choreography.Chronicle.Workflows.Listing;

/// <summary>
/// Folds events through the reducer of a read model the way Chronicle would, one event after the other.
/// </summary>
/// <typeparam name="TModel">The type of read model.</typeparam>
public class ReadModelScenario<TModel>
    where TModel : class
{
    static readonly DateTimeOffset _start = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    readonly object _reducer = Activator.CreateInstance(typeof(TModel).Assembly.GetTypes().Single(type => type.IsClass && type.GetInterfaces().Contains(typeof(IReducerFor<TModel>))))!;
    int _applied;

    public ReadModelScenario() => Given = new(this);

    public EventsGiven Given { get; }

    public TModel Instance { get; private set; } = null!;

    public class EventsGiven(ReadModelScenario<TModel> scenario)
    {
        public EventSourceGiven ForEventSource(string eventSourceId) => new(scenario, eventSourceId);
    }

    public class EventSourceGiven(ReadModelScenario<TModel> scenario, string eventSourceId)
    {
        public Task Events(params object[] events)
        {
            foreach (var @event in events)
            {
                scenario.Apply(@event, eventSourceId);
            }

            return Task.CompletedTask;
        }
    }

    void Apply(object @event, string eventSourceId)
    {
        var method = _reducer.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(candidate => candidate.Name == "On" && candidate.GetParameters()[0].ParameterType == @event.GetType());
        var context = EventContext.Empty with { EventSourceId = new EventSourceId(eventSourceId), Occurred = _start.AddMinutes(_applied++) };
        Instance = (TModel)method.Invoke(_reducer, [@event, Instance, context])!;
    }
}
