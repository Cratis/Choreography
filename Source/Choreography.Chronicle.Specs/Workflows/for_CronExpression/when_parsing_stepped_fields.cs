// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Chronicle.Workflows.for_CronExpression;

public class when_parsing_stepped_fields : Specification
{
    static bool MatchesMinute(string expression, int minute) => CronExpression.Parse(expression).Matches(new DateTimeOffset(2026, 3, 2, 9, minute, 0, TimeSpan.Zero));

    [Fact] void should_step_from_a_start_to_the_maximum() => MatchesMinute("5/20 * * * *", 45).ShouldBeTrue();
    [Fact] void should_not_match_before_the_start() => MatchesMinute("5/20 * * * *", 4).ShouldBeFalse();
    [Fact] void should_step_within_a_range() => MatchesMinute("10-30/10 * * * *", 20).ShouldBeTrue();
    [Fact] void should_not_step_beyond_a_range() => MatchesMinute("10-30/10 * * * *", 40).ShouldBeFalse();
    [Fact] void should_match_a_single_value_without_step() => MatchesMinute("7 * * * *", 8).ShouldBeFalse();
    [Fact] void should_refuse_a_reversed_range() => Catch.Exception(() => CronExpression.Parse("30-10 * * * *")).ShouldBeOfExactType<InvalidCronExpression>();
    [Fact] void should_refuse_a_value_below_the_minimum() => Catch.Exception(() => CronExpression.Parse("* * 0 * *")).ShouldBeOfExactType<InvalidCronExpression>();
    [Fact] void should_refuse_day_of_week_eight() => Catch.Exception(() => CronExpression.Parse("* * * * 8")).ShouldBeOfExactType<InvalidCronExpression>();
}
