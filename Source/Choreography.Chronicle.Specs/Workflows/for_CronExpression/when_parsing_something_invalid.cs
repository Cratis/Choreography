// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Chronicle.Workflows.for_CronExpression;

public class when_parsing_something_invalid : Specification
{
    [Fact] void should_refuse_too_few_fields() => Catch.Exception(() => CronExpression.Parse("* * * *")).ShouldBeOfExactType<InvalidCronExpression>();
    [Fact] void should_refuse_a_value_out_of_range() => Catch.Exception(() => CronExpression.Parse("60 * * * *")).ShouldBeOfExactType<InvalidCronExpression>();
    [Fact] void should_refuse_text() => Catch.Exception(() => CronExpression.Parse("a * * * *")).ShouldBeOfExactType<InvalidCronExpression>();
    [Fact] void should_refuse_a_step_of_zero() => Catch.Exception(() => CronExpression.Parse("*/0 * * * *")).ShouldBeOfExactType<InvalidCronExpression>();
}
