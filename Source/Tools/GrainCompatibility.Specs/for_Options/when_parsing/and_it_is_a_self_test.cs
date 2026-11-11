// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Tools.GrainCompatibility.for_Options.when_parsing;

public class and_it_is_a_self_test : Specification
{
    Options _result;

    void Because() => _result = Options.Parse(["--self-test", "--github", "--baseline-name", "x", "--baseline", "old.txt"]);

    [Fact] void should_self_test() => _result.SelfTest.ShouldBeTrue();
    [Fact] void should_not_require_the_current_contract() => _result.Current.ShouldBeNull();
    [Fact] void should_drop_the_baseline() => _result.Baseline.ShouldBeNull();
    [Fact] void should_keep_the_other_options() => (_result.GitHub, _result.BaselineName).ShouldEqual((true, "x"));
}
