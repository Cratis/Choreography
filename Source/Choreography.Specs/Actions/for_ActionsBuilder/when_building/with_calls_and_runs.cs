// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Choreography.Actions.for_ActionsBuilder.when_building;

public class with_calls_and_runs : given.a_builder_with_services
{
    ActionResult _result;

    async Task Because()
    {
        _builder
            .Call<ServiceForTesting>(service => service.Do("typed"))
            .Call<ServiceForTesting>(service => service.DoAsync("typed-async"))
            .Call(services => services.GetRequiredService<ServiceForTesting>().Do("provider"))
            .Call(services => services.GetRequiredService<ServiceForTesting>().DoAsync("provider-async"))
            .Run(context => _service.Do($"run-{context.Identity.Key}"))
            .Run(context => _service.DoAsync("run-async"));

        _result = await Execute();
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_make_the_calls_in_the_order_they_were_added() => _service.Calls.ShouldEqual(["typed", "typed-async", "provider", "provider-async", "run-a-key", "run-async"]);
}
