// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.StateMachines;
using Cratis.Orleans.Workflows;
using Cratis.Orleans.Workflows.Runs;
using Cratis.Orleans.Workflows.Triggers;
using Microsoft.Extensions.Options;
using Orleans.TestKit;

namespace Cratis.Orleans.Chronicle.Workflows.for_WorkflowStateExtensions.given;

public class a_state_in_a_machine : Specification
{
    protected IWorkflows Workflows = null!;
    protected TestKitSilo Silo = new();
    protected WorkflowScope? StartedIn;
    protected WorkflowTrigger? Started;

    protected IState<string> StateOf<TMachine>(string key)
        where TMachine : Grain, IGrainWithStringKey, IStateMachine<string>
    {
        Workflows = Substitute.For<IWorkflows>();
        Workflows.Start(Arg.Do<WorkflowScope>(scope => StartedIn = scope), Arg.Any<WorkflowId>(), Arg.Do<WorkflowTrigger>(trigger => Started = trigger))
            .Returns(Task.FromResult(WorkflowAdmission.To(new WorkflowRunId("run-1"))));
        Silo.AddService(Workflows);
        Silo.AddService(Options.Create(new EventSourcedWorkflowsOptions { ScopeFor = @namespace => new($"scope-of-{@namespace.Value}") }));
        var machine = Silo.CreateGrainAsync<TMachine>(key).GetAwaiter().GetResult();
        return new TheState(machine);
    }

    sealed class TheState(IStateMachine<string> machine) : IState<string>
    {
        public IStateMachine<string> StateMachine => machine;

        public Task<bool> CanTransitionTo<TTargetState>(string state) => Task.FromResult(true);

        public Task<string> OnEnter(string state) => Task.FromResult(state);

        public Task<string> OnLeave(string state) => Task.FromResult(state);
    }
}
