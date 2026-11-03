// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_SemanticVersion.when_comparing_versions;

public class and_versions_have_prerelease_identifiers : Specification
{
    [Fact] void should_order_numeric_identifiers_numerically() => ShouldOrder("2.0.0-rc.2", "2.0.0-rc.10");
    [Fact] void should_order_a_shorter_set_of_identifiers_first() => ShouldOrder("2.0.0-alpha", "2.0.0-alpha.1");
    [Fact] void should_order_a_pre_release_before_its_release() => ShouldOrder("1.0.0-rc.1", "1.0.0");
    [Fact] void should_order_numeric_identifiers_before_text() => ShouldOrder("2.0.0-2", "2.0.0-alpha");
    [Fact] void should_order_text_identifiers_alphabetically() => ShouldOrder("2.0.0-alpha", "2.0.0-beta");
    [Fact] void should_order_numeric_identifiers_too_large_for_a_long() => ShouldOrder("2.0.0-999999999999999999999999999999", "2.0.0-1000000000000000000000000000000");
    [Fact] void should_ignore_build_metadata_after_a_pre_release() => ShouldBeEqual("2.0.0-rc.2+build.1", "2.0.0-rc.2+build.10");
    [Fact] void should_ignore_build_metadata_on_a_release() => ShouldBeEqual("2.0.0+build.10", "2.0.0+build.2");

    static void ShouldOrder(string lower, string higher)
    {
        var older = SemanticVersion.Parse(lower);
        var newer = SemanticVersion.Parse(higher);
        older.CompareTo(newer).ShouldBeLessThan(0);
        newer.CompareTo(older).ShouldBeGreaterThan(0);
        (older < newer).ShouldBeTrue();
        (older <= newer).ShouldBeTrue();
        (newer > older).ShouldBeTrue();
        (newer >= older).ShouldBeTrue();
    }

    static void ShouldBeEqual(string left, string right)
    {
        var first = SemanticVersion.Parse(left);
        var second = SemanticVersion.Parse(right);
        first.CompareTo(second).ShouldEqual(0);
        second.CompareTo(first).ShouldEqual(0);
        (first < second).ShouldBeFalse();
        (first > second).ShouldBeFalse();
    }
}
