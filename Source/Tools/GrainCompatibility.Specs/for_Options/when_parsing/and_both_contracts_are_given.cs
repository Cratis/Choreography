// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Tools.GrainCompatibility.for_Options.when_parsing;

public class and_both_contracts_are_given : Specification
{
    Options _result;

    void Because() => _result = Options.Parse(["--baseline", "old.txt", "--current", "new.txt"]);

    [Fact] void should_keep_the_baseline() => _result.Baseline.ShouldEqual("old.txt");
    [Fact] void should_keep_the_current() => _result.Current.ShouldEqual("new.txt");
    [Fact] void should_name_the_baseline_baseline() => _result.BaselineName.ShouldEqual("baseline");
    [Fact] void should_not_emit_github_commands() => _result.GitHub.ShouldBeFalse();
    [Fact] void should_not_self_test() => _result.SelfTest.ShouldBeFalse();
}
