// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs;

/// <summary>
/// The facts a run records about itself.
/// </summary>
/// <remarks>
/// Their type names and JSON shapes are a contract: stores that already hold them are read back by name. Add a
/// fact by adding a record here; never rename one or change what an existing one carries. A journal that keeps facts
/// as events uses the bare CLR type name as the event type identifier.
/// </remarks>
public static class WorkflowRunFacts
{
    /// <summary>
    /// Gets every fact type a run can record.
    /// </summary>
    public static readonly IReadOnlyList<Type> Types =
    [
        typeof(WorkflowRunAdmitted),
        typeof(WorkflowNodeScheduled),
        typeof(WorkflowNodeOperationCompleted),
        typeof(WorkflowNodeSucceeded),
        typeof(WorkflowNodeSkipped),
        typeof(WorkflowNodeAttemptFailed),
        typeof(WorkflowNodeFailed),
        typeof(WorkflowNodeOutcomeAmbiguous),
        typeof(WorkflowNodesUnreachable),
        typeof(WorkflowBranchesFannedOut),
        typeof(WorkflowBranchesJoined),
        typeof(WorkflowWaitOpened),
        typeof(WorkflowWaitResolved),
        typeof(WorkflowWaitCancelled),
        typeof(WorkflowRunSucceeded),
        typeof(WorkflowRunFailed),
        typeof(WorkflowRunCancelled),
        typeof(WorkflowRunNeedsReconciliation),
        typeof(WorkflowNodeInputPinned),
        typeof(WorkflowJobStopped),
        typeof(WorkflowNodeReconciled)
    ];

    static readonly Dictionary<string, Type> _byName = Types.ToDictionary(NameOf);

    /// <summary>
    /// Gets the name a fact type is known by in a journal - the bare CLR type name.
    /// </summary>
    /// <param name="type">The fact type.</param>
    /// <returns>The name.</returns>
    public static string NameOf(Type type) => type.Name;

    /// <summary>
    /// Gets the fact type known by a name.
    /// </summary>
    /// <param name="name">The name, as given by <see cref="NameOf"/>.</param>
    /// <returns>The type, or null when no fact has that name.</returns>
    public static Type? TypeNamed(string name) => _byName.GetValueOrDefault(name);
}
