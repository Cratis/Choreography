// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.for_Job.given;
using Moq;
using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_Job.when_externalizing;

/// <summary>
/// A retired activation takes its watchdog with it, and storage trouble can keep a replacement from activating, so it asks the
/// jobs manager - which touches no job storage - to bring the job back.
/// </summary>
public class and_the_activation_retires : given.a_job_with_a_plan_store
{
    Mock<IJobsManager> _manager;
    SomeJob _retired;

    async Task Establish()
    {
        _planRemovalFails = true;
        await _job.Start(new());
        _storedJobStepStates.Remove(StoredJobStep(_firstId));
        await ActivateFrom(_job.WrittenStates.Last(state => state.Status is JobStatus.StartingSteps));
        _manager = _silo.AddProbe<IJobsManager>(0, new JobsManagerKey(_jobKey.Scope, _jobKey.Namespace));
        _manager.Setup(_ => _.RecoverRetiredJob(It.IsAny<JobId>())).Returns(Task.CompletedTask);
        _retired = _job;
        _retired.RejectWrite = state => state.Status is JobStatus.Running;
    }

    async Task Because() => _result = await _retired.Resume();

    [Fact] void should_retire_the_activation() => _silo.GrainRuntime.Mock.Verify(runtime => runtime.DeactivateOnIdle(It.IsAny<IGrainContext>()), Times.Once);
    [Fact] void should_not_take_the_job_forward() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_ask_the_jobs_manager_to_recover_it_once() => _manager.Verify(_ => _.RecoverRetiredJob(_jobId), Times.Once);
    [Fact] async Task should_refuse_a_later_resume() => (await _retired.Resume()).IsSuccess.ShouldBeFalse();
}
