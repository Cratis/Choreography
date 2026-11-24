// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CS0618 // The forwarder is obsolete on purpose; the spec proves it still delegates.

using Cratis.Arc.Availability;
using Cratis.Arc.Availability.Audiences;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Choreography.Availability.for_AvailabilityServiceCollectionExtensions;

public class when_adding_availability : Specification
{
    IServiceProvider _provider;

    void Because()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAudienceSource>(new InMemoryAudienceSource());
        services.AddSingleton<IDeclaredAudiences>(new DeclaredAudiences("Plus"));
        services.AddSingleton(new Cratis.Arc.Availability.AvailabilityCatalog());
        Microsoft.Extensions.DependencyInjection.AvailabilityServiceCollectionExtensions.AddAvailability(services, _ =>
        {
            _.EnvironmentName = "Local";
            _.GrantAllDeclaredAudiences = true;
        });
        _provider = services.BuildServiceProvider();
    }

    [Fact] void should_resolve_the_arc_evaluator() => _provider.GetService<IAvailabilityEvaluator>().ShouldBeOfExactType<Cratis.Arc.Availability.AvailabilityEvaluator>();
    [Fact] void should_resolve_the_arc_audience_resolver() => _provider.GetService<IAudienceResolver>().ShouldBeOfExactType<Cratis.Arc.Availability.Audiences.AudienceResolver>();
    [Fact] void should_carry_the_environment_name_to_the_arc_options() => _provider.GetService<Cratis.Arc.Availability.AvailabilityOptions>()!.EnvironmentName.ShouldEqual("Local");
    [Fact] void should_carry_the_switch_to_the_arc_options() => _provider.GetService<Cratis.Arc.Availability.AvailabilityOptions>()!.GrantAllDeclaredAudiences.ShouldBeTrue();
}
