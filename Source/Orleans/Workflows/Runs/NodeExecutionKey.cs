// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Graphs;

namespace Cratis.Orleans.Workflows.Runs;

/// <summary>
/// Identifies one execution of a node within a run - the node, the branch it runs in, and which visit
/// of that node in that branch it is.
/// </summary>
/// <param name="Node">The <see cref="WorkflowNodeId"/>.</param>
/// <param name="Branch">The <see cref="WorkflowBranchKey"/>.</param>
/// <param name="Visit">The visit, counted from one.</param>
public record NodeExecutionKey(WorkflowNodeId Node, WorkflowBranchKey Branch, int Visit)
{
    const char Separator = '/';

    /// <summary>
    /// Parses a key from its text.
    /// </summary>
    /// <param name="text">The text, as produced by <see cref="ToString"/>.</param>
    /// <returns>The <see cref="NodeExecutionKey"/>.</returns>
    /// <exception cref="MalformedNodeExecutionKey">The text does not describe a node execution.</exception>
    public static NodeExecutionKey Parse(string text)
    {
        var parts = text.Split(Separator);
        if (parts.Length != 3 || !int.TryParse(parts[2], out var visit))
        {
            throw new MalformedNodeExecutionKey(text);
        }

        return new(Uri.UnescapeDataString(parts[0]), Uri.UnescapeDataString(parts[1]), visit);
    }

    /// <summary>
    /// Creates the key of the first visit of a node in the root branch.
    /// </summary>
    /// <param name="node">The <see cref="WorkflowNodeId"/>.</param>
    /// <returns>The <see cref="NodeExecutionKey"/>.</returns>
    public static NodeExecutionKey RootOf(WorkflowNodeId node) => new(node, WorkflowBranchKey.Root, 1);

    /// <inheritdoc/>
    /// <remarks>
    /// Node and branch are escaped, so the text is unambiguous whatever the identities contain.
    /// </remarks>
    public override string ToString() =>
        $"{Uri.EscapeDataString(Node.Value)}{Separator}{Uri.EscapeDataString(Branch.Value)}{Separator}{Visit}";
}
