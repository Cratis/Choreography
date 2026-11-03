// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Contracts;

/// <summary>
/// A reference to one version of a data contract.
/// </summary>
/// <param name="Id">The <see cref="WorkflowContractId"/>.</param>
/// <param name="Version">The <see cref="WorkflowContractVersion"/>.</param>
public record WorkflowContractReference(WorkflowContractId Id, WorkflowContractVersion Version)
{
    /// <summary>
    /// The wildcard contract - any payload is accepted.
    /// </summary>
    public static readonly WorkflowContractReference Any = new("any", 0);
}
