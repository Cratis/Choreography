// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Runs;

namespace Cratis.Choreography.Workflows.Journal;

/// <summary>
/// A fact read back from a run's journal, with the time it was recorded.
/// </summary>
/// <param name="Fact">The fact - one of the records in <see cref="WorkflowRunFacts"/>.</param>
/// <param name="Occurred">When it was recorded.</param>
public record WorkflowRecordedFact(object Fact, DateTimeOffset Occurred);
