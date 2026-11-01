// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Hosting.for_CratisOrleansOptions;

public class when_running_locally : Specification
{
    CratisOrleansOptions _options = null!;

    void Because() => _options = CratisOrleansOptions.Local();

    [Fact] void should_cluster_on_localhost() => _options.Clustering.ShouldEqual(ClusteringMode.Localhost);
    [Fact] void should_keep_grain_state_in_memory() => _options.Storage.ShouldEqual(GrainStorageMode.InMemory);
    [Fact] void should_not_host_workflows_unless_asked_to() => _options.Workflows.ShouldBeFalse();
}
