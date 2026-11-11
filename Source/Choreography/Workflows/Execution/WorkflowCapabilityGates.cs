// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Choreography.Workflows.Graphs;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Workflows.Execution;

/// <summary>
/// Represents an implementation of <see cref="IWorkflowCapabilityGates"/> that holds one semaphore per limit rule.
/// </summary>
/// <remarks>
/// A rule is a capability id, matching it exactly, or a prefix ending in <c lang="csharp">*</c> that matches every capability starting with it - so
/// <c lang="csharp">ability:Slack.*</c> is one gate shared by all the abilities of a system. An exact rule beats a prefix, and the longest prefix
/// beats a shorter one. A limit of zero or less is no limit.
/// </remarks>
/// <param name="options">The <see cref="WorkflowsOptions"/>.</param>
public class WorkflowCapabilityGates(IOptions<WorkflowsOptions> options) : IWorkflowCapabilityGates
{
    static readonly IDisposable _noLease = new NoLease();

    readonly ConcurrentDictionary<string, SemaphoreSlim?> _gates = new();
    readonly ConcurrentDictionary<string, SemaphoreSlim> _shared = new();

    /// <inheritdoc/>
    public async ValueTask<IDisposable> Enter(CapabilityReference capability, CancellationToken cancellationToken)
    {
        var gate = _gates.GetOrAdd(capability.Id.Value, Resolve);
        if (gate is null)
        {
            return _noLease;
        }

        await gate.WaitAsync(cancellationToken);
        return new Lease(gate);
    }

    SemaphoreSlim? Resolve(string capability)
    {
        var limits = options.Value.CapabilityLimits;
        if (limits.TryGetValue(capability, out var exact))
        {
            return Gate(capability, exact);
        }

        var prefix = limits.Keys
            .Where(rule => rule.EndsWith('*') && capability.StartsWith(rule[..^1], StringComparison.Ordinal))
            .OrderByDescending(rule => rule.Length)
            .FirstOrDefault();
        return prefix is null ? null : Gate(prefix, limits[prefix]);
    }

    SemaphoreSlim? Gate(string rule, int limit) =>
        limit <= 0 ? null : _shared.GetOrAdd(rule, (_, size) => new SemaphoreSlim(size, size), limit);

    sealed class NoLease : IDisposable
    {
        public void Dispose()
        {
        }
    }

    sealed class Lease(SemaphoreSlim gate) : IDisposable
    {
        int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                gate.Release();
            }
        }
    }
}
