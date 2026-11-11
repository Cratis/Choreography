// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Tools.GrainCompatibility.for_Options.when_parsing;

public class and_every_option_is_given : Specification
{
    Options _result;

    void Because() => _result = Options.Parse(["--github", "--baseline-name", "v1.2", "--current", "new.txt", "--baseline", "old.txt"]);

    [Fact] void should_keep_the_baseline_name() => _result.BaselineName.ShouldEqual("v1.2");
    [Fact] void should_emit_github_commands() => _result.GitHub.ShouldBeTrue();
    [Fact] void should_keep_both_contracts() => (_result.Baseline, _result.Current).ShouldEqual(("old.txt", "new.txt"));
}
