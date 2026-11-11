// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows;

/// <summary>
/// Represents the options of the workflow engine.
/// </summary>
public class WorkflowsOptions
{
    /// <summary>
    /// Gets or sets the scope of the jobs manager the wave jobs of a run are started in, for the default
    /// <see cref="Scopes.IWorkflowExecutionScopes"/>. Defaults to <c lang="csharp">workflows</c>.
    /// </summary>
    public string JobsScope { get; set; } = "workflows";

    /// <summary>
    /// Gets the scopes the engine sweeps and recovers jobs for, besides those a <see cref="Sweeping.IWorkflowScopeSource"/> names
    /// and those seen on the silo.
    /// </summary>
    public ISet<string> Scopes { get; } = new HashSet<string>();

    /// <summary>
    /// Gets or sets whether a run wakes itself: while a retry or a wait deadline is pending, its grain keeps a timer
    /// and a reminder so it acts when the time comes, with no sweep. Defaults to true.
    /// </summary>
    public bool WakeRunsItself { get; set; } = true;

    /// <summary>
    /// Gets or sets how often the sweep asks every active run of every known scope whether it has anything to do. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan SweepInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets how long after the sweep starts the first pass runs. Defaults to 20 seconds.
    /// </summary>
    public TimeSpan SweepStartupDelay { get; set; } = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Gets or sets how long a run may be idle before the sweep takes it forward again from its recorded history. Defaults to 15 minutes.
    /// </summary>
    public TimeSpan IdleBeforeResume { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Gets the most performer calls of a capability that may be in flight on this silo at once, keyed by capability id - or by a prefix
    /// ending in <c lang="csharp">*</c> for a gate several capabilities share, such as <c lang="csharp">ability:Slack.*</c> for one external system.
    /// A capability no entry covers is not limited. A call that waits for room waits inside its attempt's timeout.
    /// </summary>
    public IDictionary<string, int> CapabilityLimits { get; } = new Dictionary<string, int>();

    /// <summary>
    /// Gets or sets whether the sweep runs on this silo at all. Defaults to true.
    /// </summary>
    public bool Sweep { get; set; } = true;
}
