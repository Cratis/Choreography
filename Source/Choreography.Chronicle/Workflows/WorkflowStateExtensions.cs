// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Chronicle.StateMachines;
using Cratis.Choreography.StateMachines;
using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Choreography.Workflows.Triggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Chronicle.Workflows;

/// <summary>
/// Extension methods for starting a workflow when a state machine enters a state.
/// </summary>
public static class WorkflowStateExtensions
{
    /// <summary>
    /// Starts a workflow for the state machine the state belongs to - call it from <c lang="csharp">OnEnter</c>.
    /// </summary>
    /// <typeparam name="TStoredState">The type of state the machine stores.</typeparam>
    /// <param name="state">The <see cref="IState{TStoredState}"/> being entered.</param>
    /// <param name="workflow">The <see cref="WorkflowId"/> of the workflow to start.</param>
    /// <param name="payload">What the run is given as its trigger payload. Defaults to nothing.</param>
    /// <param name="deduplication">
    /// The key that makes entering the state again start the run again, or not. The default is the state and the machine's key,
    /// so a state that is entered again - a retried transition, an activation that finds the machine in it - finds the run
    /// the first entry started; pass a key that is distinct for every entry that should run again.
    /// </param>
    /// <param name="scope">The scope of the workflow. Defaults, for an event-sourced state machine, to the scope of its namespace.</param>
    /// <returns>The <see cref="WorkflowAdmission"/>.</returns>
    /// <remarks>
    /// The subject of the run is the machine - its type is the subject kind and its key the subject id - and the key is also the
    /// run's correlation, so a signal raised for the machine reaches the run.
    /// </remarks>
    /// <exception cref="StateMachineIsNotAGrain">The state's machine is not a grain, so it has no key and no services.</exception>
    public static Task<WorkflowAdmission> StartWorkflow<TStoredState>(
        this IState<TStoredState> state,
        WorkflowId workflow,
        WorkflowData? payload = default,
        WorkflowDeduplicationKey? deduplication = default,
        WorkflowScope? scope = default)
    {
        if (state.StateMachine is not Grain machine)
        {
            throw new StateMachineIsNotAGrain(state.StateMachine.GetType());
        }

        var services = machine.GrainContext.ActivationServices;
        var key = machine.GetPrimaryKeyString();
        var subject = key;
        if (scope is null && machine is IEventSourcedStateMachine)
        {
            var machineKey = StateMachineKey.Parse(key);
            scope = services.GetRequiredService<IOptions<EventSourcedWorkflowsOptions>>().Value.ScopeFor(machineKey.Namespace);
            subject = machineKey.EventSourceId.Value;
        }

        var kind = machine.GetType().Name;
        var trigger = new WorkflowTrigger(
            WorkflowTriggerKind.StageHook,
            new TriggerSourceKey($"{kind}.{state.GetType().Name}"),
            deduplication ?? new WorkflowDeduplicationKey($"{kind}.{state.GetType().Name}:{key}"),
            new WorkflowSubjectReference(new WorkflowSubjectKind(kind), new WorkflowSubjectId(subject)),
            WorkflowContractReference.Any,
            payload ?? WorkflowData.Empty,
            [],
            new WorkflowCorrelationKey(subject),
            WorkflowCausationKey.None,
            WorkflowActor.System,
            services.GetService<TimeProvider>()?.GetUtcNow() ?? DateTimeOffset.UtcNow);

        return services.GetRequiredService<IWorkflows>().Start(scope ?? WorkflowScope.Default, workflow, trigger);
    }
}
