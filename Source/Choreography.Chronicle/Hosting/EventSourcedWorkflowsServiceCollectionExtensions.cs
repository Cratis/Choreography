// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Chronicle.StateMachines;
using Cratis.Choreography.Chronicle.Workflows;
using Cratis.Choreography.Hosting;
using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Journal;
using Cratis.Choreography.Workflows.Scopes;
using Cratis.Chronicle;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Chronicle.Hosting;

/// <summary>
/// Extension methods for hosting the workflow engine with its runs kept as events in Chronicle.
/// </summary>
public static class EventSourcedWorkflowsServiceCollectionExtensions
{
    /// <summary>
    /// Adds the workflow engine with a journal that appends the facts of runs to Chronicle's event log.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">Optional configuration of the <see cref="EventSourcedWorkflowsOptions"/> and the <see cref="WorkflowsOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This does everything <c lang="csharp">AddCratisChoreographyWorkflows</c> does, and replaces the journal on grain storage and the
    /// execution scopes with Chronicle's: the run's facts are events with the run as event source, the wave jobs of a scope live in
    /// the jobs manager of its event store namespace. The run facts are registered with the Chronicle client as event types named
    /// by their type names, the run listing and the open waits as read models, and a reactor per declared event trigger.
    /// </para>
    /// <para>
    /// No grain storage provider is needed for the runs. The run index the sweep uses still keeps a small state, so register the
    /// provider named <see cref="WellKnownGrainStorageProviders.Workflows"/>.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddEventSourcedWorkflows(this IServiceCollection services, Action<EventSourcedWorkflowsOptions>? configure = default)
    {
        services.AddOptions<EventSourcedWorkflowsOptions>().Configure(options => configure?.Invoke(options));
        services.AddCratisChoreographyWorkflows();
        services.TryAddSingleton<ITypes>(_ => new Cratis.Types.Types());
        services.TryAddSingleton<IClientArtifactsProvider>(DefaultClientArtifactsProvider.Default);
        services.TryAddSingleton<IStateMachineEventStores, StateMachineEventStores>();
        services.Replace(ServiceDescriptor.Singleton<IWorkflowRunJournal, ChronicleWorkflowRunJournal>());
        services.Replace(ServiceDescriptor.Singleton<IWorkflowExecutionScopes, ChronicleWorkflowExecutionScopes>());
        services.TryAddSingleton<IWorkflowSignals, WorkflowSignals>();
        services.TryAddSingleton<IWorkflowCompletions, WorkflowCompletions>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, WorkflowScheduleService>());

        // Open generic, constrained to ChronicleOptions: the container only applies it to the options types that
        // derive from it - ChronicleOptions, ChronicleClientOptions, the ASP.NET Core options - and skips the rest.
        services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IConfigureOptions<>), typeof(RegisterEventSourcedWorkflows<>)));
        return services;
    }

    /// <summary>
    /// Adds the workflow engine with its runs kept as events in Chronicle to a silo.
    /// </summary>
    /// <param name="builder">The silo builder.</param>
    /// <param name="configure">Optional configuration of the <see cref="EventSourcedWorkflowsOptions"/>.</param>
    /// <returns>The silo builder for chaining.</returns>
    public static ISiloBuilder AddEventSourcedWorkflows(this ISiloBuilder builder, Action<EventSourcedWorkflowsOptions>? configure = default)
    {
        builder.Services.AddEventSourcedWorkflows(configure);
        return builder;
    }
}
