// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.Jobs;

namespace Cratis.Choreography.Storage.Sql.for_JobStepStorage.given;

/// <summary>
/// A job step state that adds its own properties to the base state, like a real job step's state does.
/// </summary>
public class a_derived_job_step_state : JobStepState
{
    /// <summary>
    /// Gets or sets a value only the derived state knows about.
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a number only the derived state knows about.
    /// </summary>
    public int Progress { get; set; }
}
