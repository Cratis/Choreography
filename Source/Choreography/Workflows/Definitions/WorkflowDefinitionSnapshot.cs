// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Definitions;

/// <summary>
/// The active version of a workflow, as a run is admitted with it.
/// </summary>
/// <param name="Workflow">The <see cref="WorkflowId"/>.</param>
/// <param name="Area">The <see cref="WorkflowAreaKey"/> that owns it.</param>
/// <param name="Version">The <see cref="WorkflowVersion"/>.</param>
/// <param name="Subject">The <see cref="WorkflowSubjectKind"/> its runs are about.</param>
/// <param name="Graph">The <see cref="WorkflowGraph"/>.</param>
/// <param name="CatalogVersion">The <see cref="WorkflowCatalogVersion"/> it was authored against.</param>
/// <param name="Capabilities">The <see cref="CapabilityPin">provider pins</see> of the capabilities it uses.</param>
public record WorkflowDefinitionSnapshot(
    WorkflowId Workflow,
    WorkflowAreaKey Area,
    WorkflowVersion Version,
    WorkflowSubjectKind Subject,
    WorkflowGraph Graph,
    WorkflowCatalogVersion CatalogVersion,
    IEnumerable<CapabilityPin> Capabilities);
