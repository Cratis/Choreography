// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage;
using Cratis.Choreography.Storage.Patching;
using Cratis.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Runtime.Hosting;
using Orleans.Serialization.Configuration;

namespace Cratis.Choreography.Hosting;

/// <summary>
/// Extension methods for co-hosting an Orleans silo with the Cratis job system.
/// </summary>
/// <remarks>
/// The co-hosted silo uses localhost clustering by default; set <see cref="CratisChoreographyOptions.Clustering"/> to
/// <see cref="ClusteringMode.MongoDB"/> for durable cluster membership when running more than one instance.
/// </remarks>
public static class CratisChoreographySiloBuilderExtensions
{
    /// <summary>
    /// Adds the co-hosted Orleans silo with the Cratis job system and its grain storage.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/> to configure.</param>
    /// <param name="options">The <see cref="CratisChoreographyOptions"/> to use.</param>
    /// <returns>The same <see cref="WebApplicationBuilder"/> for chaining.</returns>
    /// <remarks>
    /// <para>
    /// <c lang="csharp">Orleans.Providers.MongoDB</c> has no Orleans 10 release - 9.5.0 is a 9.x binary - so only the parts of it
    /// proven safe on the 10.x runtime are used: cluster membership and grain storage, and nothing else.
    /// </para>
    /// <para>
    /// The reminder table is where that version skew bites. Pointing the 10.x <c lang="csharp">LocalReminderService</c> at the
    /// 9.x <c lang="csharp">MongoReminderTable</c> makes the reminder service stop responding: the first
    /// <c lang="csharp">RegisterOrUpdateReminder</c> never completes, the calling grain's request times out, the reminder row is
    /// never written, and no reminder-backed grain ever ticks. Orleans' own in-memory reminder service is 10.x end
    /// to end and works in both modes, so it is used unconditionally. Reminders then do not survive a full cluster
    /// restart. The jobs recovery participant rehydrates durable jobs after activation and member failure, for
    /// every key supplied by singleton IJobsManagerKeys providers. The default only knows keys seen on this silo;
    /// hosts supply a complete key catalog to recover namespaces this silo has never served.
    /// </para>
    /// </remarks>
    public static WebApplicationBuilder AddCratisChoreography(this WebApplicationBuilder builder, CratisChoreographyOptions? options = default)
    {
        options ??= new CratisChoreographyOptions();
        if (!options.Enabled)
        {
            return builder;
        }

        builder.Host.UseOrleans(silo =>
        {
            AddCoreServices(silo, options);
            ConfigureClustering(silo, options);
            ConfigureGrainStorage(silo, options);
            AddJobStorageProviders(silo);
            AddOptionalFeatures(silo, options);
        });

        return builder;
    }

    /// <summary>
    /// Adds automatic recovery of interrupted jobs to a separately configured silo.
    /// </summary>
    /// <param name="silo">The silo builder.</param>
    /// <returns>The silo builder for chaining.</returns>
    public static ISiloBuilder AddCratisChoreographyJobsRecovery(this ISiloBuilder silo)
    {
        silo.Services.TryAddSingleton<ITypes>(_ => new Cratis.Types.Types());
        silo.Services.TryAddSingleton<JobsManagerKeys>();
        silo.Services.TryAddTransient<IInstancesOf<IJobsManagerKeys>>(services => new JobsManagerKeyProviders(
            services.GetRequiredService<ITypes>().FindMultiple<IJobsManagerKeys>()
                .Select(type => (type, (Func<IJobsManagerKeys>)(() => (IJobsManagerKeys)ActivatorUtilities.GetServiceOrCreateInstance(services, type))))
                .ToArray(),
            services.GetRequiredService<ILogger<JobsRecovery>>()));
        silo.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILifecycleParticipant<ISiloLifecycle>, JobsRecovery>());
        return silo;
    }

    /// <summary>
    /// Adds an <see cref="IJobAdmission"/> that is asked before a job's execution begins or begins again.
    /// </summary>
    /// <param name="silo">The silo builder.</param>
    /// <typeparam name="TAdmission">The <see cref="IJobAdmission"/> implementation, registered as a singleton.</typeparam>
    /// <returns>The silo builder for chaining.</returns>
    /// <remarks>
    /// A silo without a registration admits every job, so register it on every silo that can host the job types it
    /// concerns. Adding the same implementation twice registers it once.
    /// </remarks>
    public static ISiloBuilder AddJobAdmission<TAdmission>(this ISiloBuilder silo)
        where TAdmission : class, IJobAdmission
    {
        silo.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IJobAdmission, TAdmission>());
        return silo;
    }

    static void AddCoreServices(ISiloBuilder silo, CratisChoreographyOptions options)
    {
        // Orleans 10 registers its propagation filters with AddSingleton, so preserve a host's existing registration.
        // The filter type is internal to Orleans and can only be identified by its full name.
        if (!silo.Services.Any(descriptor => descriptor.ServiceType == typeof(IIncomingGrainCallFilter) &&
            descriptor.ImplementationType?.FullName == "Orleans.Runtime.ActivityPropagationIncomingGrainCallFilter"))
        {
            silo.AddActivityPropagation();
        }

        silo.Configure<ClusterOptions>(clusterOptions =>
        {
            clusterOptions.ClusterId = options.ClusterId;
            clusterOptions.ServiceId = options.ServiceId;
        });

        // Serializable types in client-facing assemblies can make Orleans' configuration analyzer fail at
        // startup on types this silo never serializes. Skip the analysis unless it is asked for.
        silo.Services.Configure<TypeManifestOptions>(typeManifestOptions => typeManifestOptions.EnableConfigurationAnalysis = false);

        // Serializers for ConceptAs types crossing grain boundaries - job ids, names, statuses and the
        // like. Registering them here means a host never has to know about them.
        silo.Services.AddCratisChoreographySerializers();

        // The job system's own collaborators: type discovery, the job type registry, the step throttle and
        // the options both read. TryAdd keeps host registrations in charge where they exist.
        silo.Services.TryAddSingleton<ITypes>(_ => new Cratis.Types.Types());
        silo.Services.TryAddSingleton<IJobTypes, JobTypes>();
        silo.Services.TryAddSingleton<IJobs, Jobs.Jobs>();
        silo.Services.TryAddSingleton<IJobStepThrottle, JobStepThrottle>();
        silo.Services.TryAddSingleton<IOptions<JobsOptions>>(_ => Options.Create(new JobsOptions()));
        silo.AddCratisChoreographyJobsRecovery();
    }

    static void ConfigureClustering(ISiloBuilder silo, CratisChoreographyOptions options)
    {
        if (options.Storage == GrainStorageMode.MongoDB || options.Clustering == ClusteringMode.MongoDB)
        {
            silo.UseMongoDBClient(options.ConnectionString);
        }

        if (options.Clustering == ClusteringMode.MongoDB)
        {
            silo.UseMongoDBClustering(membershipOptions =>
            {
                membershipOptions.DatabaseName = options.DatabaseName;
                membershipOptions.Strategy = MongoDBMembershipStrategy.SingleDocument;
            });
        }
        else
        {
            silo.UseLocalhostClustering();
        }
    }

    static void ConfigureGrainStorage(ISiloBuilder silo, CratisChoreographyOptions options)
    {
        if (options.Storage == GrainStorageMode.MongoDB)
        {
            // Durable grain state for the job system - a running job survives a silo going away mid-flight.
            silo.AddMongoDBGrainStorage(WellKnownGrainStorageProviders.Jobs, grainStorageOptions => grainStorageOptions.DatabaseName = options.DatabaseName);
            silo.AddMongoDBGrainStorage(WellKnownGrainStorageProviders.JobSteps, grainStorageOptions => grainStorageOptions.DatabaseName = options.DatabaseName);
            if (options.Workflows)
            {
                silo.AddMongoDBGrainStorage(WellKnownGrainStorageProviders.Workflows, grainStorageOptions => grainStorageOptions.DatabaseName = options.DatabaseName);
            }
        }
        else
        {
            // The local configuration: jobs, steps and workflow runs live in memory of the one silo.
            silo.Services.TryAddSingleton<IJobsStorage, InMemoryJobsStorage>();
            if (options.Workflows)
            {
                silo.AddMemoryGrainStorage(WellKnownGrainStorageProviders.Workflows);
            }
        }

        // Never the 9.x MongoDB reminder table on the 10.x runtime - it hangs the reminder service outright
        // (see the remarks on AddCratisChoreography).
        silo.UseInMemoryReminderService();
    }

    static void AddJobStorageProviders(ISiloBuilder silo)
    {
        // The job system's grain storage providers resolve jobs storage per scope and namespace through
        // IJobsStorage - the MongoDB jobs storage package registers that. Register the providers under the
        // names the job grains ask for by name.
        silo.Services.AddGrainStorage<Jobs.JobGrainStorageProvider>(WellKnownGrainStorageProviders.Jobs, (serviceProvider, _) =>
            new(serviceProvider.GetRequiredService<IJobsStorage>(), serviceProvider.GetRequiredService<ILogger<Jobs.JobGrainStorageProvider>>()));
        silo.Services.AddGrainStorage<Jobs.JobStepGrainStorageProvider>(WellKnownGrainStorageProviders.JobSteps, (serviceProvider, _) =>
            new(serviceProvider.GetRequiredService<IJobsStorage>()));
    }

    static void AddOptionalFeatures(ISiloBuilder silo, CratisChoreographyOptions options)
    {
        if (options.Workflows)
        {
            silo.AddCratisChoreographyWorkflows(options.ConfigureWorkflows);
        }

        if (options.Patching)
        {
            silo.AddCratisChoreographyPatching(options.ConfigurePatching);
            if (options.Storage == GrainStorageMode.InMemory)
            {
                silo.Services.TryAddSingleton<IPatchStorage, InMemoryPatchStorage>();
            }
        }
    }
}
