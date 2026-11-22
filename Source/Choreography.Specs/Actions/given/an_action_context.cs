// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Actions.given;

public abstract class an_action_context : Specification
{
    protected ActionContext _context;
    protected IServiceProvider _services;

    void Establish()
    {
        _services = Substitute.For<IServiceProvider>();
        _context = new(new(GrainKind.StateMachine, "a-type", "a-key"), _services);
    }
}
