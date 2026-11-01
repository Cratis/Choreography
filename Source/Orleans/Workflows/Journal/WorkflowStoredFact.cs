// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Journal;

/// <summary>
/// A fact as the grain storage journal keeps it.
/// </summary>
[GenerateSerializer]
public class WorkflowStoredFact
{
    /// <summary>
    /// Gets or sets the name of the fact's type.
    /// </summary>
    [Id(0)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the fact as JSON.
    /// </summary>
    [Id(1)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the fact was recorded, in UTC ticks.
    /// </summary>
    [Id(2)]
    public long OccurredTicks { get; set; }
}
