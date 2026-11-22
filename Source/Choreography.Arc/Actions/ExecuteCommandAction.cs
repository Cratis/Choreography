// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Choreography.Actions;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Choreography.Arc.Actions;

/// <summary>
/// Represents an <see cref="IAction"/> that executes a Cratis Arc command through the <see cref="ICommandPipeline"/>.
/// </summary>
/// <remarks>
/// The action succeeds when the command succeeds and fails when it is not authorized, not valid or throws, with what the
/// pipeline reported as the reason. The <see cref="ICommandPipeline"/> is resolved from the services of the grain
/// executing the action, so register Arc's command handling there.
/// </remarks>
/// <param name="create">Creates the command to execute from the <see cref="ActionContext"/>.</param>
public class ExecuteCommandAction(Func<ActionContext, object> create) : IAction
{
    /// <inheritdoc/>
    public async Task<ActionResult> Execute(ActionContext context, CancellationToken cancellationToken)
    {
        var command = create(context);
        var pipeline = context.Services.GetRequiredService<ICommandPipeline>();
        var result = pipeline is ICommandPipelineWithCancellation cancellable
            ? await cancellable.Execute(command, context.Services, null, cancellationToken)
            : await pipeline.Execute(command, context.Services);

        return result.IsSuccess ? ActionResult.Succeeded() : ActionResult.Failed(ReasonFor(command, result));
    }

    static string ReasonFor(object command, CommandResult result)
    {
        var reasons = new List<string>();
        if (!result.IsAuthorized)
        {
            reasons.Add(string.IsNullOrEmpty(result.AuthorizationFailureReason) ? "not authorized" : result.AuthorizationFailureReason);
        }

        reasons.AddRange(result.ValidationResults.Select(validation => validation.Message));
        reasons.AddRange(result.ExceptionMessages);
        return $"Command '{command.GetType().Name}' failed: {string.Join("; ", reasons)}";
    }
}
