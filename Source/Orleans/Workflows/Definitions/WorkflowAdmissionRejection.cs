// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Definitions;

/// <summary>
/// Why a workflow trigger was not admitted as a run.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record WorkflowAdmissionRejection(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The rejection for a workflow with no active version to run.
    /// </summary>
    public static readonly WorkflowAdmissionRejection NoActiveVersion = new("The workflow has no active version");

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="WorkflowAdmissionRejection"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WorkflowAdmissionRejection(string value) => new(value);
}
