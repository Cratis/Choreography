// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Tools.GrainCompatibility.for_Options.when_parsing;

public class and_an_option_has_no_value : Specification
{
    Exception _result;

    void Because() => _result = Catch.Exception(() => Options.Parse(["--baseline", "--current", "new.txt"]));

    [Fact] void should_say_the_arguments_are_invalid() => _result.ShouldBeOfExactType<InvalidArguments>();
    [Fact] void should_name_the_option() => _result.Message.ShouldContain("'--baseline' needs a value");
}
