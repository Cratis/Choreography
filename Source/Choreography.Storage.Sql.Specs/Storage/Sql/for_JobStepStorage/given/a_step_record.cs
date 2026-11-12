// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Storage.Sql.for_JobStepStorage.given;

/// <summary>
/// A record held by the step state.
/// </summary>
/// <param name="Name">The name.</param>
/// <param name="Count">The count.</param>
public record a_step_record(a_step_concept Name, int Count);
