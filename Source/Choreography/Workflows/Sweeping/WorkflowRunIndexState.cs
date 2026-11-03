// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Sweeping;

/// <summary>
/// The grain storage state of a <see cref="IWorkflowRunIndex"/>.
/// </summary>
[GenerateSerializer]
public class WorkflowRunIndexState
{
    /// <summary>
    /// Gets or sets the runs that are not over.
    /// </summary>
    [Id(0)]
    public ISet<string> Runs { get; set; } = new HashSet<string>();
}
