// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Triggers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Choreography.Actions;

/// <summary>
/// Represents a fluent builder of the actions a state or a workflow node performs, in the order they are added.
/// </summary>
/// <remarks>
/// Packages add their own actions as extension methods, such as Append in Cratis.Choreography.Chronicle.Actions.
/// </remarks>
public class ActionsBuilder
{
    readonly List<IAction> _actions = [];

    /// <summary>
    /// Gets the actions added so far.
    /// </summary>
    public IReadOnlyList<IAction> Actions => _actions;

    /// <summary>
    /// Add an action.
    /// </summary>
    /// <param name="action">The <see cref="IAction"/> to add.</param>
    /// <returns>The builder for continuation.</returns>
    public ActionsBuilder Add(IAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _actions.Add(action);
        return this;
    }

    /// <summary>
    /// Run code that has the <see cref="ActionContext"/>.
    /// </summary>
    /// <param name="code">The code to run.</param>
    /// <returns>The builder for continuation.</returns>
    public ActionsBuilder Run(Action<ActionContext> code) => Add(CodeAction.Run(code));

    /// <summary>
    /// Run asynchronous code that has the <see cref="ActionContext"/>.
    /// </summary>
    /// <param name="code">The code to run.</param>
    /// <returns>The builder for continuation.</returns>
    public ActionsBuilder Run(Func<ActionContext, Task> code) => Add(CodeAction.Run(code));

    /// <summary>
    /// Call a method on a service, resolved from the services of the grain at the time of the call.
    /// </summary>
    /// <typeparam name="TService">The type of service to resolve.</typeparam>
    /// <param name="call">The call to make.</param>
    /// <returns>The builder for continuation.</returns>
    public ActionsBuilder Call<TService>(Action<TService> call)
        where TService : notnull => Add(CodeAction.Run(context => call(context.Services.GetRequiredService<TService>())));

    /// <summary>
    /// Call an asynchronous method on a service, resolved from the services of the grain at the time of the call.
    /// </summary>
    /// <typeparam name="TService">The type of service to resolve.</typeparam>
    /// <param name="call">The call to make.</param>
    /// <returns>The builder for continuation.</returns>
    public ActionsBuilder Call<TService>(Func<TService, Task> call)
        where TService : notnull => Add(CodeAction.Run(context => call(context.Services.GetRequiredService<TService>())));

    /// <summary>
    /// Call code with the services of the grain, to resolve whatever it needs.
    /// </summary>
    /// <param name="call">The call to make.</param>
    /// <returns>The builder for continuation.</returns>
    public ActionsBuilder Call(Action<IServiceProvider> call) => Add(CodeAction.Run(context => call(context.Services)));

    /// <summary>
    /// Call asynchronous code with the services of the grain, to resolve whatever it needs.
    /// </summary>
    /// <param name="call">The call to make.</param>
    /// <returns>The builder for continuation.</returns>
    public ActionsBuilder Call(Func<IServiceProvider, Task> call) => Add(CodeAction.Run(context => call(context.Services)));

    /// <summary>
    /// Run a process - an operating system command - without a shell.
    /// </summary>
    /// <param name="fileName">The executable to run.</param>
    /// <param name="arguments">The arguments, each passed as one argument.</param>
    /// <returns>The builder for continuation.</returns>
    public ActionsBuilder RunProcess(string fileName, params string[] arguments) => Add(new ProcessAction(fileName, arguments));

    /// <summary>
    /// Run a process - an operating system command - without a shell.
    /// </summary>
    /// <param name="fileName">The executable to run.</param>
    /// <param name="arguments">The arguments, each passed as one argument.</param>
    /// <param name="workingDirectory">Optional working directory.</param>
    /// <param name="timeout">Optional time the process may run for.</param>
    /// <returns>The builder for continuation.</returns>
    public ActionsBuilder RunProcess(string fileName, IEnumerable<string> arguments, string? workingDirectory = default, TimeSpan? timeout = default) =>
        Add(new ProcessAction(fileName, arguments, workingDirectory, timeout));

    /// <summary>
    /// Switch the state machine to another state. Only meaningful for a state machine's actions.
    /// </summary>
    /// <typeparam name="TState">The type of state to switch to.</typeparam>
    /// <returns>The builder for continuation.</returns>
    public ActionsBuilder TransitionTo<TState>() => Add(TransitionStateAction.To<TState>());

    /// <summary>
    /// Start a workflow.
    /// </summary>
    /// <param name="workflow">The workflow to start.</param>
    /// <param name="trigger">Creates the <see cref="WorkflowTrigger"/> from the <see cref="ActionContext"/>.</param>
    /// <param name="scope">Optional <see cref="WorkflowScope"/> to start it in.</param>
    /// <returns>The builder for continuation.</returns>
    public ActionsBuilder StartWorkflow(WorkflowId workflow, Func<ActionContext, WorkflowTrigger> trigger, WorkflowScope? scope = default) =>
        Add(new StartWorkflowAction(workflow, trigger, scope));

    /// <summary>
    /// Build the added actions into one <see cref="IAction"/> that executes them in order.
    /// </summary>
    /// <returns>The <see cref="CompositeAction"/>.</returns>
    public IAction Build() => new CompositeAction(_actions);
}
