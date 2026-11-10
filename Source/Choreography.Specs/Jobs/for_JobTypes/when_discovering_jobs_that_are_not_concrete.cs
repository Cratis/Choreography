// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Jobs.for_JobTypes;

[Collection("JobTypes")]
public class when_discovering_jobs_that_are_not_concrete : Specification
{
    JobTypes _registry;
    ITypes _types;

    void Establish()
    {
        _types = Substitute.For<ITypes>();
        _types.FindMultiple<IJob>().Returns([typeof(ConcreteDiscoveredJob), typeof(AbstractDiscoveredJob), typeof(NullJob), typeof(INullJobWithSomeRequest)]);
    }

    void Because() => _registry = new JobTypes(_types);

    [Fact] void should_only_register_the_concrete_job() => _registry.All.ShouldContainOnly(new JobType("concrete-job"));

    [JobType("concrete-job")]
    public class ConcreteDiscoveredJob : NullJobWithSomeRequest;

    public abstract class AbstractDiscoveredJob : NullJobWithSomeRequest;
}
