// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Choreography.Actions.for_ActionsBuilder.given;

public abstract class a_builder_with_services : Specification
{
    protected ActionsBuilder _builder;
    protected ActionContext _context;
    protected ServiceForTesting _service;

    void Establish()
    {
        _builder = new();
        _service = new();
        var services = new ServiceCollection().AddSingleton(_service).BuildServiceProvider();
        _context = new(new(GrainKind.StateMachine, "a-type", "a-key"), services);
    }

    protected Task<ActionResult> Execute() => _builder.Build().Execute(_context, CancellationToken.None);

    public class ServiceForTesting
    {
        public List<string> Calls { get; } = [];

        public void Do(string what) => Calls.Add(what);

        public Task DoAsync(string what)
        {
            Calls.Add(what);
            return Task.CompletedTask;
        }
    }
}
