// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Actions;

/// <summary>
/// Defines something a state machine or a workflow does - run code, execute a command, append an event, switch state,
/// start a workflow, or several of these in a <see cref="CompositeAction"/>.
/// </summary>
public interface IAction
{
    /// <summary>
    /// Execute the action.
    /// </summary>
    /// <param name="context">The <see cref="ActionContext"/> the action executes within.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that is cancelled when the action should stop.</param>
    /// <returns>The <see cref="ActionResult"/>.</returns>
    /// <remarks>
    /// An action reports a failure it expects as a failed <see cref="ActionResult"/>. An exception is an unexpected failure.
    /// </remarks>
    Task<ActionResult> Execute(ActionContext context, CancellationToken cancellationToken);
}
