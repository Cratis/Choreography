// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_SemanticVersion;

public class when_trying_to_parse_invalid_versions : Specification
{
    static bool Accepts(string version) => SemanticVersion.TryParse(version, out _);

    [Fact] void should_reject_empty_string() => Accepts(string.Empty).ShouldBeFalse();
    [Fact] void should_reject_too_few_parts() => Accepts("1.2").ShouldBeFalse();
    [Fact] void should_reject_too_many_parts() => Accepts("1.2.3.4").ShouldBeFalse();
    [Fact] void should_reject_non_numeric_part() => Accepts("1.a.3").ShouldBeFalse();
    [Fact] void should_reject_leading_zero() => Accepts("1.02.3").ShouldBeFalse();
    [Fact] void should_reject_numeric_overflow() => Accepts("1.2.99999999999").ShouldBeFalse();
    [Fact] void should_reject_empty_prerelease() => Accepts("1.2.3-").ShouldBeFalse();
    [Fact] void should_reject_numeric_prerelease_with_leading_zero() => Accepts("1.2.3-01").ShouldBeFalse();
    [Fact] void should_reject_empty_prerelease_identifier() => Accepts("1.2.3-alpha..1").ShouldBeFalse();
    [Fact] void should_reject_empty_build_metadata() => Accepts("1.2.3+").ShouldBeFalse();
    [Fact] void should_reject_invalid_build_character() => Accepts("1.2.3+a_b").ShouldBeFalse();
    [Fact] void should_accept_build_identifier_with_leading_zero() => Accepts("1.2.3+001").ShouldBeTrue();
    [Fact] void should_accept_prerelease_with_hyphen() => Accepts("1.2.3-alpha-1").ShouldBeTrue();
}
