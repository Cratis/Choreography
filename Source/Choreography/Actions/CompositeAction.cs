// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Actions;

/// <summary>
/// Represents an <see cref="IAction"/> that executes several actions, in order.
/// </summary>
/// <remarks>
/// The actions run one after the other and the first one that fails stops the rest and fails the composite. The output
/// is the output of the last action that ran. A composite is an <see cref="IAction"/>, so composites nest.
/// </remarks>
/// <param name="actions">The actions to execute, in order.</param>
public class CompositeAction(IEnumerable<IAction> actions) : IAction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeAction"/> class.
    /// </summary>
    /// <param name="actions">The actions to execute, in order.</param>
    public CompositeAction(params IAction[] actions)
        : this((IEnumerable<IAction>)actions)
    {
    }

    /// <summary>
    /// Gets the actions, in the order they execute.
    /// </summary>
    public IReadOnlyList<IAction> Actions { get; } = [.. actions];

    /// <inheritdoc/>
    public async Task<ActionResult> Execute(ActionContext context, CancellationToken cancellationToken)
    {
        var last = ActionResult.Succeeded();
        for (var index = 0; index < Actions.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            last = await Actions[index].Execute(context, cancellationToken);
            if (!last.IsSuccess)
            {
                return ActionResult.Failed($"Action {index + 1} of {Actions.Count} ({Actions[index].GetType().Name}) failed: {last.Reason}");
            }
        }

        return last;
    }
}
