// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Tools.GrainCompatibility.for_Options.when_parsing;

public class and_a_contract_is_missing : Specification
{
    Exception _result;

    void Because() => _result = Catch.Exception(() => Options.Parse(["--baseline", "old.txt"]));

    [Fact] void should_say_the_arguments_are_invalid() => _result.ShouldBeOfExactType<InvalidArguments>();
    [Fact] void should_name_both_options() => _result.Message.ShouldContain("--baseline and --current");
}
