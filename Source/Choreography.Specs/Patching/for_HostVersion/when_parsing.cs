// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_HostVersion;

public class when_parsing : Specification
{
    [Fact] void should_read_a_plain_version() => ShouldReadWithoutMetadata("19.100.2");
    [Fact] void should_read_a_version_with_build_metadata() => ShouldReadWithoutMetadata("19.100.2+abc1234");
    [Fact] void should_read_a_pre_release_version_with_build_metadata() => ShouldReadWithoutMetadata("19.100.2-rc.1+abc1234");
    [Fact] void should_not_set_an_empty_version() => HostVersion.Parse(string.Empty).ShouldEqual(SemanticVersion.NotSet);
    [Fact] void should_not_set_text_that_is_not_a_version() => HostVersion.Parse("not-a-version").ShouldEqual(SemanticVersion.NotSet);
    [Fact] void should_not_set_a_version_with_two_parts() => HostVersion.Parse("1.0").ShouldEqual(SemanticVersion.NotSet);

    static void ShouldReadWithoutMetadata(string informational)
    {
        var result = HostVersion.Parse(informational);
        result.Major.ShouldEqual(19);
        result.Minor.ShouldEqual(100);
        result.Patch.ShouldEqual(2);
        result.BuildMetadata.ShouldBeEmpty();
    }
}
