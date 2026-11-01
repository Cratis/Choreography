// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Registrations;
using Cratis.Orleans.Chronicle.Workflows.Listing;
using Cratis.Orleans.Workflows;
using Cratis.Orleans.Workflows.Runs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Orleans.Chronicle.Workflows;

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
/// </remarks>
internal sealed class RegisterEventSourcedWorkflows<TOptions>(IServiceProvider services) : IConfigureOptions<TOptions>
    where TOptions : ChronicleOptions
{
    static readonly ConditionalWeakTable<ExplicitArtifacts, object> _registered = [];

    /// <inheritdoc/>
    public void Configure(TOptions options)
    {
        if (!_registered.TryAdd(options.ExplicitArtifacts, new()))
        {
            return;
        }

        var workflows = services.GetRequiredService<IOptions<EventSourcedWorkflowsOptions>>().Value;
        Register(options.ExplicitArtifacts, workflows, services);
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
