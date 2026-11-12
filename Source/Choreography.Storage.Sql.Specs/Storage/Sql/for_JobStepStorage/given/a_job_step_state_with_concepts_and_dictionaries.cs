// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
namespace Cratis.Choreography.Storage.Sql.for_JobStepStorage.given;

/// <summary>
/// A job step state holding concepts, a list of concepts and dictionaries keyed by a concept.
/// </summary>
public class a_job_step_state_with_concepts_and_dictionaries : JobStepState
{
    /// <summary>
    /// Gets or sets a concept.
    /// </summary>
    public a_step_concept Concept { get; set; } = new(string.Empty);

    /// <summary>
    /// Gets or sets a list of concepts.
    /// </summary>
    public IList<a_step_concept> Concepts { get; set; } = [];

    /// <summary>
    /// Gets or sets counts by concept.
    /// </summary>
    public IDictionary<a_step_concept, long> Counts { get; set; } = new Dictionary<a_step_concept, long>();

    /// <summary>
    /// Gets or sets records by concept.
    /// </summary>
    public IDictionary<a_step_concept, IList<a_step_record>> Records { get; set; } = new Dictionary<a_step_concept, IList<a_step_record>>();

    /// <summary>
    /// Creates a populated state.
    /// </summary>
    /// <param name="jobStepIdentifier">The identity of the step.</param>
    /// <returns>The state.</returns>
    public static a_job_step_state_with_concepts_and_dictionaries Populated(JobStepIdentifier jobStepIdentifier) =>
        new()
        {
            Id = jobStepIdentifier,
            Type = new JobStepType("some-step-type"),
            Name = (JobStepName)"some step",
            Status = JobStepStatus.Running,
            IsPrepared = true,
            Concept = new("first"),
            Concepts = [new("one"), new("two")],
            Counts = new Dictionary<a_step_concept, long> { [new("a")] = 3, [new("b")] = 5 },
            Records = new Dictionary<a_step_concept, IList<a_step_record>>
            {
                [new("a")] = [new(new("x"), 1), new(new("y"), 2)]
            }
        };
}
