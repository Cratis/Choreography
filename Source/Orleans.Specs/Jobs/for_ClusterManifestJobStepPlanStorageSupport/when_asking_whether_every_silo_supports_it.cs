// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Net;
using Moq;
using Orleans.Metadata;

namespace Cratis.Orleans.Jobs.for_ClusterManifestJobStepPlanStorageSupport;

public class when_asking_whether_every_silo_supports_it : Specification
{
    static readonly GrainType _jobType = GrainType.Create("job");

    bool _allSupport;
    bool _oneLacksIt;
    bool _noneHosts;

    static ImmutableDictionary<string, string> Properties(params string[] keyAndValue) =>
        keyAndValue.Length == 0 ? ImmutableDictionary.Create<string, string>(StringComparer.Ordinal) : ImmutableDictionary.Create<string, string>(StringComparer.Ordinal).Add(keyAndValue[0], keyAndValue[1]);

    static GrainManifest SiloWith(bool supported) => new(
        new Dictionary<GrainType, GrainProperties> { [_jobType] = new(supported ? Properties(JobStepPlanStorageSupportAttribute.PropertyName, JobStepPlanStorageSupportAttribute.PropertyValue) : Properties()) }.ToImmutableDictionary(),
        []);

    static bool Ask(params GrainManifest[] silos)
    {
        var manifests = silos
            .Select((manifest, index) => (Address: SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111 + index), 1), Manifest: manifest))
            .ToImmutableDictionary(_ => _.Address, _ => _.Manifest);
        var provider = new Mock<IClusterManifestProvider>();
        provider.SetupGet(_ => _.Current).Returns(new ClusterManifest(new(1, 1), manifests, [.. silos]));
        return new ClusterManifestJobStepPlanStorageSupport(provider.Object).IsSupportedByEverySilo(_jobType);
    }

    void Because()
    {
        _allSupport = Ask(SiloWith(true), SiloWith(true));
        _oneLacksIt = Ask(SiloWith(true), SiloWith(false));
        _noneHosts = Ask(new GrainManifest([], []));
    }

    [Fact] void should_support_it_when_every_hosting_silo_does() => _allSupport.ShouldBeTrue();
    [Fact] void should_not_support_it_when_one_silo_lacks_it() => _oneLacksIt.ShouldBeFalse();
    [Fact] void should_not_support_it_when_no_silo_hosts_the_job() => _noneHosts.ShouldBeFalse();
}
