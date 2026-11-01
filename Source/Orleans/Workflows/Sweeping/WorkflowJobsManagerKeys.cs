// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Workflows.Scopes;

namespace Cratis.Orleans.Workflows.Sweeping;

/// <summary>
/// Supplies the jobs manager keys of every known scope, so the wave jobs of a run interrupted by a restart are recovered.
/// </summary>
/// <param name="scopes">The <see cref="WorkflowScopes"/>.</param>
/// <param name="executionScopes">The <see cref="IWorkflowExecutionScopes"/>.</param>
public class WorkflowJobsManagerKeys(WorkflowScopes scopes, IWorkflowExecutionScopes executionScopes) : IJobsManagerKeys
{
    /// <inheritdoc/>
    public async Task<IEnumerable<JobsManagerKey>> GetKeys(CancellationToken cancellationToken) =>
        (await scopes.GetAll(cancellationToken)).Select(executionScopes.JobsManagerFor).ToArray();
}
