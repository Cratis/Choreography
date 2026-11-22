// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;

namespace Cratis.Choreography.Actions;

/// <summary>
/// Represents an <see cref="IAction"/> that executes a command - an operating system process.
/// </summary>
/// <remarks>
/// <para>
/// The command runs without a shell, so the arguments are never interpreted. It succeeds when the process exits with
/// code 0 and fails otherwise, with the standard error as the reason. The output is JSON holding the exit code and the
/// standard output. The identity of the grain executing the action is passed in the CHOREOGRAPHY_GRAIN_KIND,
/// CHOREOGRAPHY_GRAIN_TYPE and CHOREOGRAPHY_GRAIN_KEY environment variables.
/// </para>
/// <para>
/// A command that is still running when the action is cancelled, or when the optional timeout passes, is killed along
/// with its child processes.
/// </para>
/// </remarks>
/// <param name="fileName">The executable to run.</param>
/// <param name="arguments">Optional arguments, each passed as one argument.</param>
/// <param name="workingDirectory">Optional working directory.</param>
/// <param name="timeout">Optional time the command may run for.</param>
public class CommandAction(string fileName, IEnumerable<string>? arguments = default, string? workingDirectory = default, TimeSpan? timeout = default) : IAction
{
    /// <inheritdoc/>
    public async Task<ActionResult> Execute(ActionContext context, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var argument in arguments ?? [])
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (workingDirectory is not null)
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        startInfo.Environment["CHOREOGRAPHY_GRAIN_KIND"] = context.Identity.Kind.ToString();
        startInfo.Environment["CHOREOGRAPHY_GRAIN_TYPE"] = context.Identity.Type;
        startInfo.Environment["CHOREOGRAPHY_GRAIN_KEY"] = context.Identity.Key;

        using var process = new Process { StartInfo = startInfo };
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeout is { } allowed)
        {
            limit.CancelAfter(allowed);
        }

        try
        {
            process.Start();
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return ActionResult.Failed($"The command '{fileName}' could not be started: {exception.Message}");
        }

        var standardOutput = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var standardError = process.StandardError.ReadToEndAsync(CancellationToken.None);

        try
        {
            await process.WaitForExitAsync(limit.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            cancellationToken.ThrowIfCancellationRequested();
            return ActionResult.Failed($"The command '{fileName}' ran longer than {timeout}");
        }

        var output = await standardOutput;
        var error = await standardError;
        return process.ExitCode == 0
            ? ActionResult.Succeeded(JsonSerializer.Serialize(new Dictionary<string, object> { ["exitCode"] = 0, ["standardOutput"] = output }))
            : ActionResult.Failed($"The command '{fileName}' exited with code {process.ExitCode}: {error.Trim()}");
    }
}
