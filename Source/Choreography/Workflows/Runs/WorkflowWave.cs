// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs;

/// <summary>The workflow wave recorded on an event.</summary>
/// <param name="Value">The wrapped value.</param>
public record WorkflowWave(int Value) : ConceptAs<int>(Value)
{
    /// <summary>Converts the underlying value.</summary>
    /// <param name="value">The value.</param>
    public static implicit operator WorkflowWave(int value) => new(value);
}
