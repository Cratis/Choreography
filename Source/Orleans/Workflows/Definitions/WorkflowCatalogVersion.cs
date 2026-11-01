// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Definitions;

/// <summary>
/// The version of the capability catalog a workflow definition was authored against.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record WorkflowCatalogVersion(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="WorkflowCatalogVersion"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WorkflowCatalogVersion(string value) => new(value);
}
