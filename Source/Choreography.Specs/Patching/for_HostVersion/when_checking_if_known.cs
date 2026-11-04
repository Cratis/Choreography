// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_HostVersion;

public class when_checking_if_known : Specification
{
    [Fact] void should_not_know_a_version_that_is_not_set() => HostVersion.IsKnown(SemanticVersion.NotSet).ShouldBeFalse();
    [Fact] void should_not_know_the_placeholder_of_an_unversioned_build() => HostVersion.IsKnown(new SemanticVersion(1, 0, 0)).ShouldBeFalse();
    [Fact] void should_know_a_released_version() => HostVersion.IsKnown(new SemanticVersion(19, 100, 0)).ShouldBeTrue();
    [Fact] void should_know_a_pre_release_of_the_placeholder() => HostVersion.IsKnown(new SemanticVersion(1, 0, 0, "rc.1")).ShouldBeTrue();
}
