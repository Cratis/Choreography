// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Definitions;
using Cratis.Choreography.Workflows.Journal;
using Cratis.Choreography.Workflows.Scopes;
using Cratis.Choreography.Workflows.Sweeping;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Cratis.Choreography.Hosting;

/// <summary>
/// Extension methods for hosting the workflow engine in a silo.
/// </summary>
public static class WorkflowsSiloBuilderExtensions
{
    /// <summary>
    /// Adds the workflow engine to a silo - the journal on grain storage, the sweep, node execution and the registrations
    /// that find the performers, definition sources and admission policies in the application.
    /// </summary>
    /// <param name="silo">The silo builder.</param>
    /// <param name="configure">Optional configuration of the <see cref="WorkflowsOptions"/>.</param>
    /// <returns>The silo builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// The run grains keep their facts in the grain storage provider named <see cref="WellKnownGrainStorageProviders.Workflows"/>,
    /// which <see cref="CratisChoreographyOptions.Workflows"/> registers; a silo configured another way registers a provider under
    /// that name itself. Wave jobs and their steps use the job system, so it must be hosted too.
    /// </para>
    /// <para>
    /// Every registration is a <c lang="csharp">TryAdd</c>: register your own <see cref="IWorkflowRunJournal"/> or
    /// <see cref="IWorkflowExecutionScopes"/> first, or use <c lang="csharp">AddEventSourcedWorkflows</c> from <c lang="csharp">Cratis.Choreography.Chronicle</c>
    /// to keep the facts in Chronicle.
    /// </para>
    /// </remarks>
    public static ISiloBuilder AddCratisChoreographyWorkflows(this ISiloBuilder silo, Action<WorkflowsOptions>? configure = default)
    {
        silo.Services.AddCratisChoreographyWorkflows(configure);
        return silo;
    }

    /// <summary>
    /// Adds the services of the workflow engine.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">Optional configuration of the <see cref="WorkflowsOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddCratisChoreographyWorkflows(this IServiceCollection services, Action<WorkflowsOptions>? configure = default)
    {
        services.AddOptions<WorkflowsOptions>().Configure(options => configure?.Invoke(options));
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ITypes>(_ => new Cratis.Types.Types());
        services.TryAddSingleton<IWorkflowRunJournal, GrainStorageWorkflowRunJournal>();
        services.TryAddSingleton<IWorkflows, WorkflowStarter>();
        services.TryAddSingleton<IWorkflowExecutionScopes, DefaultWorkflowExecutionScopes>();
        services.TryAddSingleton<WorkflowScopes>();
        services.TryAddSingleton<IWorkflowRunSweeper, WorkflowRunSweeper>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, WorkflowRunSweepService>());

        services.TryAddScoped<IWorkflowNodePerformers, WorkflowNodePerformers>();
        services.TryAddScoped<IWorkflowDefinitionSource, CompositeWorkflowDefinitionSource>();
        services.TryAddEnumerable(ServiceDescriptor.Transient<IWorkflowAdmissionPolicy, StructuralAdmissionPolicy>());
        services.TryAddTransient<IInstancesOf<IWorkflowNodePerformer>, DiscoveredInstances<IWorkflowNodePerformer>>();
        services.TryAddTransient<IInstancesOf<IWorkflowDefinitions>, DiscoveredInstances<IWorkflowDefinitions>>();
        services.TryAddTransient<IInstancesOf<IWorkflowAdmissionPolicy>, DiscoveredInstances<IWorkflowAdmissionPolicy>>();
        return services;
    }
}
