// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Graphs;
using Cratis.Types;

namespace Cratis.Choreography.Workflows.Adapters;

/// <summary>
/// Represents an implementation of <see cref="IWorkflowNodePerformers"/>, over every discovered performer.
/// </summary>
/// <param name="performers">The discovered <see cref="IWorkflowNodePerformer">performers</see>.</param>
/// <remarks>
/// Two performers claiming the same capability is a mistake in the code, not something to resolve at
/// run time by picking one - so construction fails immediately.
/// </remarks>
public class WorkflowNodePerformers(IInstancesOf<IWorkflowNodePerformer> performers) : IWorkflowNodePerformers
{
    readonly Dictionary<CapabilityReference, IWorkflowNodePerformer> _byCapability = Index(performers);

    /// <inheritdoc/>
    public IWorkflowNodePerformer? For(CapabilityReference capability) =>
        _byCapability.TryGetValue(capability, out var performer) ? performer : null;

    static Dictionary<CapabilityReference, IWorkflowNodePerformer> Index(IEnumerable<IWorkflowNodePerformer> performers)
    {
        var byCapability = new Dictionary<CapabilityReference, IWorkflowNodePerformer>();
        foreach (var performer in performers)
        {
            if (!byCapability.TryAdd(performer.Capability, performer))
            {
                throw new MoreThanOneWorkflowNodePerformer(performer.Capability);
            }
        }

        return byCapability;
    }
}
