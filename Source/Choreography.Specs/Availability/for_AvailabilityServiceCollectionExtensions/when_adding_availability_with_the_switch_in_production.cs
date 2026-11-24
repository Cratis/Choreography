// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CS0618 // The forwarder is obsolete on purpose; the spec proves it still delegates.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Choreography.Availability.for_AvailabilityServiceCollectionExtensions;

public class when_adding_availability_with_the_switch_in_production : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() =>
        Microsoft.Extensions.DependencyInjection.AvailabilityServiceCollectionExtensions.AddAvailability(new ServiceCollection(), _ =>
        {
            _.EnvironmentName = "Production";
            _.GrantAllDeclaredAudiences = true;
        }));

    [Fact] void should_refuse_at_startup_with_the_arc_exception() => _error.ShouldBeOfExactType<Cratis.Arc.Availability.EnvironmentSwitchRefusedException>();
}
