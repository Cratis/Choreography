// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_SemanticVersion;

public class when_ordering_versions : Specification
{
    static int Compare(string left, string right) => Math.Sign(SemanticVersion.Parse(left).CompareTo(SemanticVersion.Parse(right)));

    [Fact] void should_order_by_major() => Compare("2.0.0", "1.9.9").ShouldEqual(1);
    [Fact] void should_order_by_minor() => Compare("1.2.0", "1.3.0").ShouldEqual(-1);
    [Fact] void should_order_by_patch() => Compare("1.2.4", "1.2.3").ShouldEqual(1);
    [Fact] void should_treat_equal_versions_as_equal() => Compare("1.2.3", "1.2.3").ShouldEqual(0);
    [Fact] void should_ignore_build_metadata() => Compare("1.2.3+a", "1.2.3+b").ShouldEqual(0);
    [Fact] void should_order_release_after_prerelease() => Compare("1.2.3", "1.2.3-rc.1").ShouldEqual(1);
    [Fact] void should_order_prerelease_before_release() => Compare("1.2.3-rc.1", "1.2.3").ShouldEqual(-1);
    [Fact] void should_order_numeric_identifiers_numerically() => Compare("1.0.0-alpha.2", "1.0.0-alpha.10").ShouldEqual(-1);
    [Fact] void should_order_numeric_identifiers_before_alphanumeric() => Compare("1.0.0-1", "1.0.0-alpha").ShouldEqual(-1);
    [Fact] void should_order_alphanumeric_identifiers_ordinally() => Compare("1.0.0-beta", "1.0.0-alpha").ShouldEqual(1);
    [Fact] void should_order_longer_prerelease_after_shorter() => Compare("1.0.0-alpha.1", "1.0.0-alpha").ShouldEqual(1);
    [Fact] void should_order_any_version_after_null() => SemanticVersion.Parse("0.0.1").CompareTo(null).ShouldEqual(1);
    [Fact] void should_order_any_version_after_null_object() => SemanticVersion.Parse("0.0.1").CompareTo((object?)null).ShouldEqual(1);
    [Fact] void should_support_operators() => (SemanticVersion.Parse("1.0.0") < SemanticVersion.Parse("1.0.1") && SemanticVersion.Parse("1.0.1") >= SemanticVersion.Parse("1.0.1")).ShouldBeTrue();
}
