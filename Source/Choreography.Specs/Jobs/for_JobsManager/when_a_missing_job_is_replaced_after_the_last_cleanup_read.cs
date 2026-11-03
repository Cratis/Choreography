// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Jobs.for_JobsManager;

public class when_a_missing_job_is_replaced_after_the_last_cleanup_read : given.a_manager_with_a_stale_plan_cleanup
{
    Task Because() => CleanupAcrossReplacement();

    [Fact] Task should_keep_the_new_plan_complete_and_loadable() => AssertReplacement();
    [Fact] Task should_remove_only_the_orphaned_incarnation() => AssertOldRemoved();
}
