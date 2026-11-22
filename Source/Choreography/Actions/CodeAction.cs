// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Actions;

/// <summary>
/// Represents an <see cref="IAction"/> that runs code.
/// </summary>
/// <param name="code">The code to run.</param>
public class CodeAction(Func<ActionContext, CancellationToken, Task<ActionResult>> code) : IAction
{
    /// <summary>
    /// Create a <see cref="CodeAction"/> from asynchronous code that succeeds unless it throws.
    /// </summary>
    /// <param name="code">The code to run.</param>
    /// <returns>The <see cref="CodeAction"/>.</returns>
    public static CodeAction Run(Func<ActionContext, Task> code) => new(async (context, _) =>
    {
        await code(context);
        return ActionResult.Succeeded();
    });

    /// <summary>
    /// Create a <see cref="CodeAction"/> from code that succeeds unless it throws.
    /// </summary>
    /// <param name="code">The code to run.</param>
    /// <returns>The <see cref="CodeAction"/>.</returns>
    public static CodeAction Run(Action<ActionContext> code) => new((context, _) =>
    {
        code(context);
        return Task.FromResult(ActionResult.Succeeded());
    });

    /// <inheritdoc/>
    public Task<ActionResult> Execute(ActionContext context, CancellationToken cancellationToken) => code(context, cancellationToken);
}
