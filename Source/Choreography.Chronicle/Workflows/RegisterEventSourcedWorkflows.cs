// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Choreography.Chronicle.Workflows.Listing;
using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Chronicle.Workflows;

/// <summary>
/// Registers what the event-sourced workflow engine needs with the Chronicle client: the run facts as event types, the
/// read models and the reactors that admit runs on events.
/// </summary>
/// <typeparam name="TOptions">The type of Chronicle client options.</typeparam>
/// <param name="services">The <see cref="IServiceProvider"/> the engine's options and services are resolved from.</param>
/// <remarks>
/// The run facts are plain records in the core package, which does not know Chronicle, so they carry no event type attribute.
/// They are registered here, explicitly, with an event type identifier that is the bare type name - exactly what an
/// attribute without arguments would have given them. That is what keeps the facts already in a store readable.
/// <para>
/// Configuring the <see cref="EventSourcedWorkflowsOptions"/> may itself read Chronicle options, for instance to take the
/// event store name from them. An instance of <typeparamref name="TOptions"/> built while the same thread is already
/// registering on another instance of <typeparamref name="TOptions"/> is only read by that configuration, and the options
/// manager keeps the outer instance, so the inner one is left without the engine's artifacts instead of building the workflow
/// options again, which would recurse until the stack overflows. The guard is per options type: an instance of another
/// Chronicle options type built along the way is the one its options manager keeps, so it still gets the artifacts.
/// </para>
/// </remarks>
internal sealed class RegisterEventSourcedWorkflows<TOptions>(IServiceProvider services) : IConfigureOptions<TOptions>
    where TOptions : ChronicleOptions
{
    static readonly ConditionalWeakTable<ExplicitArtifacts, object> _registered = [];

    [ThreadStatic]
    static bool _registering;

    /// <inheritdoc/>
    public void Configure(TOptions options)
    {
        if (_registering || !_registered.TryAdd(options.ExplicitArtifacts, new()))
        {
            return;
        }

        _registering = true;
        try
        {
            var workflows = services.GetRequiredService<IOptions<EventSourcedWorkflowsOptions>>().Value;
            Register(options.ExplicitArtifacts, workflows, services);
        }
        finally
        {
            _registering = false;
        }
    }

    /// <summary>
    /// Registers the engine's artifacts.
    /// </summary>
    /// <param name="artifacts">The <see cref="ExplicitArtifacts"/> of the Chronicle client.</param>
    /// <param name="workflows">The <see cref="EventSourcedWorkflowsOptions"/>.</param>
    /// <param name="services">The <see cref="IServiceProvider"/> the reactors resolve the engine from.</param>
    internal static void Register(ExplicitArtifacts artifacts, EventSourcedWorkflowsOptions workflows, IServiceProvider services)
    {
        foreach (var fact in WorkflowRunFacts.Types)
        {
            artifacts.RegisterEventType(fact, builder => builder.WithId(new EventTypeId(WorkflowRunFacts.NameOf(fact))));
        }

        if (workflows.ReadModels)
        {
            artifacts.RegisterReducer<WorkflowRunReducer>()
                .RegisterProjection<OpenWorkflowWait>(OpenWorkflowWait.Define);
        }

        foreach (var trigger in workflows.EventTriggers)
        {
            artifacts.RegisterReactor(
                trigger.Reactor,
                builder => trigger.Define(
                    builder,
                    async (workflowTrigger, context) =>
                    {
                        if (workflowTrigger is not null)
                        {
                            await services.GetRequiredService<IWorkflows>().Start(workflows.ScopeFor(context.Namespace), trigger.Workflow, workflowTrigger);
                        }
                    }));
        }
    }
}
