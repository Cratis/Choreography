// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Chronicle.Workflows.for_CronExpression;

public class when_a_field_is_refused : Specification
{
    Exception _outside = null!;
    Exception _zeroStep = null!;

    void Because()
    {
        _outside = Catch.Exception(() => CronExpression.Parse("60 * * * *"))!;
        _zeroStep = Catch.Exception(() => CronExpression.Parse("*/0 * * * *"))!;
    }

    [Fact] void should_wrap_the_out_of_range_field() => _outside.ShouldBeOfExactType<InvalidCronExpression>();
    [Fact] void should_name_the_field_parameter() => _outside.Message.Contains("Parameter 'field'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_describe_the_rejected_range() => _outside.Message.Contains("'60' is outside 0-59", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_the_step_error() => _zeroStep.Message.Contains("a step must be at least one", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_step_field_parameter() => _zeroStep.Message.Contains("Parameter 'field'", StringComparison.Ordinal).ShouldBeTrue();
}
