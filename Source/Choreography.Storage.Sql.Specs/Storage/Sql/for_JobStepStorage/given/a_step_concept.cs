// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Storage.Sql.for_JobStepStorage.given;

/// <summary>
/// A concept used by the step state, as a property, in a list and as a dictionary key.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record a_step_concept(string Value) : ConceptAs<string>(Value);
