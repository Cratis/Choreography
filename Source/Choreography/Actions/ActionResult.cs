// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Actions;

/// <summary>
/// Represents the result of executing an <see cref="IAction"/>.
/// </summary>
/// <param name="IsSuccess">Whether or not the action succeeded.</param>
/// <param name="Output">The output of the action, as JSON.</param>
/// <param name="Reason">Why the action failed - empty when it succeeded.</param>
public record ActionResult(bool IsSuccess, string Output, string Reason)
{
    /// <summary>
    /// The JSON of an action that has no output.
    /// </summary>
    public const string NoOutput = "{}";

    /// <summary>
    /// Create a successful <see cref="ActionResult"/>.
    /// </summary>
    /// <param name="output">Optional output, as JSON.</param>
    /// <returns>The <see cref="ActionResult"/>.</returns>
    public static ActionResult Succeeded(string? output = null) => new(true, output ?? NoOutput, string.Empty);

    /// <summary>
    /// Create a failed <see cref="ActionResult"/>.
    /// </summary>
    /// <param name="reason">Why the action failed.</param>
    /// <returns>The <see cref="ActionResult"/>.</returns>
    public static ActionResult Failed(string reason) => new(false, NoOutput, reason);
}
