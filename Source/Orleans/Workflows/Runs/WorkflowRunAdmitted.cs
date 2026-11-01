// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Definitions;
using Cratis.Orleans.Workflows.Graphs;
using Cratis.Orleans.Workflows.Triggers;

namespace Cratis.Orleans.Workflows.Runs;

/// <summary>
/// A workflow run was admitted - the first fact in a run's journal. It pins everything the run will
/// ever execute: the workflow version, the graph, the catalog version and the provider of every
/// capability, so editing the workflow later never changes a run already in flight.
/// </summary>
/// <param name="Workflow">The <see cref="WorkflowId"/> the run belongs to.</param>
/// <param name="Area">The <see cref="WorkflowAreaKey"/> that owns the workflow.</param>
/// <param name="Version">The <see cref="WorkflowVersion"/> that runs.</param>
/// <param name="CatalogVersion">The <see cref="WorkflowCatalogVersion"/> the version was authored against.</param>
/// <param name="Capabilities">The <see cref="CapabilityPin">provider pins</see>.</param>
/// <param name="Graph">The <see cref="WorkflowGraph"/> that runs.</param>
/// <param name="Trigger">The <see cref="WorkflowTrigger"/> that started it.</param>
public record WorkflowRunAdmitted(
    WorkflowId Workflow,
    WorkflowAreaKey Area,
    WorkflowVersion Version,
    WorkflowCatalogVersion CatalogVersion,
    IEnumerable<CapabilityPin> Capabilities,
    WorkflowGraph Graph,
    WorkflowTrigger Trigger);
