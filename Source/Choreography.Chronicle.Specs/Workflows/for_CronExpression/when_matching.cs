// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Chronicle.Workflows.for_CronExpression;

public class when_matching : Specification
{
    static readonly DateTimeOffset _monday = new(2026, 3, 2, 9, 30, 0, TimeSpan.Zero);

    [Fact] void should_match_every_minute_with_stars() => CronExpression.Parse("* * * * *").Matches(_monday).ShouldBeTrue();
    [Fact] void should_match_an_exact_minute_and_hour() => CronExpression.Parse("30 9 * * *").Matches(_monday).ShouldBeTrue();
    [Fact] void should_not_match_another_minute() => CronExpression.Parse("31 9 * * *").Matches(_monday).ShouldBeFalse();
    [Fact] void should_match_a_step() => CronExpression.Parse("*/15 * * * *").Matches(_monday).ShouldBeTrue();
    [Fact] void should_not_match_between_steps() => CronExpression.Parse("*/20 * * * *").Matches(_monday).ShouldBeFalse();
    [Fact] void should_match_a_range() => CronExpression.Parse("25-35 9 * * *").Matches(_monday).ShouldBeTrue();
    [Fact] void should_match_a_list() => CronExpression.Parse("0,30 9,10 * * *").Matches(_monday).ShouldBeTrue();
    [Fact] void should_match_a_day_of_week() => CronExpression.Parse("30 9 * * 1").Matches(_monday).ShouldBeTrue();
    [Fact] void should_not_match_another_day_of_week() => CronExpression.Parse("30 9 * * 2").Matches(_monday).ShouldBeFalse();
    [Fact] void should_match_sunday_written_as_seven() => CronExpression.Parse("0 0 * * 7").Matches(new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero)).ShouldBeTrue();
    [Fact] void should_match_a_month() => CronExpression.Parse("30 9 * 3 *").Matches(_monday).ShouldBeTrue();
    [Fact] void should_match_either_day_when_both_day_fields_are_restricted() => CronExpression.Parse("30 9 15 * 1").Matches(_monday).ShouldBeTrue();
    [Fact] void should_ignore_the_seconds() => CronExpression.Parse("30 9 * * *").Matches(_monday.AddSeconds(42)).ShouldBeTrue();
    [Fact] void should_evaluate_in_utc() => CronExpression.Parse("30 9 * * *").Matches(new DateTimeOffset(2026, 3, 2, 11, 30, 0, TimeSpan.FromHours(2))).ShouldBeTrue();
}
