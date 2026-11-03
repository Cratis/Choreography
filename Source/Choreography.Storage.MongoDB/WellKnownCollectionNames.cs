// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Storage.MongoDB;

/// <summary>
/// Holds the well known collection names used by the jobs storage.
/// </summary>
public static class WellKnownCollectionNames
{
    /// <summary>
    /// The name of the collection holding job state.
    /// </summary>
    public const string Jobs = "jobs";

    /// <summary>
    /// The name of the collection holding job step state.
    /// </summary>
    public const string JobSteps = "job-steps";

    /// <summary>
    /// The name of the collection holding failed job steps.
    /// </summary>
    public const string FailedJobSteps = "failed-job-steps";

    /// <summary>
    /// The name of the collection holding the step plans that are too large to live inside the job document.
    /// </summary>
    public const string JobStepPlans = "job-step-plans";

    /// <summary>
    /// The name of the collection holding the records of applied patches.
    /// </summary>
    public const string Patches = "patches";

    /// <summary>
    /// The name of the collection holding the versions patching keeps - how far patches have come and which host last patched.
    /// </summary>
    public const string PatchVersions = "patch-versions";
}
