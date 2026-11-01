// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Runs;
using Cratis.Orleans.Workflows.Triggers;

namespace Cratis.Orleans.Workflows;

/// <summary>
/// A logical workflow, keyed by scope and workflow identity. Admissions always use its active stored definition.
/// </summary>
public interface IWorkflow : IGrainWithStringKey
{
    /// <summary>Admits a trigger through all discovered admission policies.</summary>
    /// <param name="trigger">The trigger.</param>
    /// <returns>The admission or rejection.</returns>
    Task<WorkflowAdmission> Admit(WorkflowTrigger trigger);
}
