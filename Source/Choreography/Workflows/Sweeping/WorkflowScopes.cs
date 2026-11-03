// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Workflows.Sweeping;

/// <summary>
/// Gathers the scopes the engine acts on - those configured, those a <see cref="IWorkflowScopeSource"/> names and those the silo has served.
/// </summary>
/// <param name="options">The <see cref="WorkflowsOptions"/>.</param>
/// <param name="sources">The <see cref="IWorkflowScopeSource"/> implementations.</param>
[Singleton]
public class WorkflowScopes(IOptions<WorkflowsOptions> options, IEnumerable<IWorkflowScopeSource> sources)
{
    readonly ConcurrentDictionary<string, byte> _seen = new();

    /// <summary>
    /// Remembers a scope the silo has served.
    /// </summary>
    /// <param name="scope">The <see cref="WorkflowScope"/>.</param>
    public void Remember(WorkflowScope scope) => _seen.TryAdd(scope.Value, 0);

    /// <summary>
    /// Gets every known scope.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The scopes.</returns>
    public async Task<IReadOnlyCollection<WorkflowScope>> GetAll(CancellationToken cancellationToken)
    {
        var all = new HashSet<WorkflowScope>(options.Value.Scopes.Select(scope => new WorkflowScope(scope)));
        all.UnionWith(_seen.Keys.Select(scope => new WorkflowScope(scope)));
        foreach (var source in sources)
        {
            all.UnionWith(await source.GetScopes(cancellationToken));
        }

        return all;
    }
}
