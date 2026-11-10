// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Jobs.for_JobStep.when_activating;

public class with_debounced_checkpoints : given.a_job_step_left_scheduled_by_a_dead_process
{
    protected override JobsOptions CreateOptions() => new() { StepCheckpointBatchInterval = 10 };

    [Fact] void should_register_a_checkpoint_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);

    void Destroy() => _jobStep.Dispose();
}
