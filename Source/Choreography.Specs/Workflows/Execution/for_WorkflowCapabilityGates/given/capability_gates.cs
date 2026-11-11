// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Execution.for_WorkflowCapabilityGates.given;

/// <summary>
/// Gates configured with the limits a spec names.
/// </summary>
public class capability_gates : Specification
{
    protected WorkflowsOptions Options = null!;
    protected WorkflowCapabilityGates Gates = null!;

    protected static CapabilityReference Capability(string id) => new(id, 1);

    void Establish()
    {
        Options = new WorkflowsOptions();
        Gates = new WorkflowCapabilityGates(Microsoft.Extensions.Options.Options.Create(Options));
    }
}
