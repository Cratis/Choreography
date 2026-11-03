// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_HostVersion;

public class when_parsing : Specification
{
    [Theory]
    [InlineData("19.100.2", 19, 100, 2)]
    [InlineData("19.100.2+abc1234", 19, 100, 2)]
    [InlineData("19.100.2-rc.1+abc1234", 19, 100, 2)]
    void should_read_the_version_without_build_metadata(string informational, int major, int minor, int patch)
    {
        var result = HostVersion.Parse(informational);
        result.Major.ShouldEqual(major);
        result.Minor.ShouldEqual(minor);
        result.Patch.ShouldEqual(patch);
        result.BuildMetadata.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-version")]
    [InlineData("1.0")]
    void should_not_set_a_version_that_cannot_be_parsed(string informational) => HostVersion.Parse(informational).ShouldEqual(SemanticVersion.NotSet);
}
