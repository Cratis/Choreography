// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Chronicle.Workflows.for_CronExpression;

public class when_matching_day_fields : Specification
{
    static readonly DateTimeOffset _monday = new(2026, 3, 2, 9, 30, 0, TimeSpan.Zero);

    [Fact] void should_require_the_day_of_month_with_any_weekday() => CronExpression.Parse("30 9 15 * *").Matches(_monday).ShouldBeFalse();
    [Fact] void should_require_either_restricted_day_to_match() => CronExpression.Parse("30 9 15 * 2").Matches(_monday).ShouldBeFalse();
    [Fact] void should_match_a_stepped_range_in_a_list() => CronExpression.Parse("1,10-20/5 * * * *").Matches(_monday.AddMinutes(-15)).ShouldBeTrue();
    [Fact] void should_not_match_between_steps_in_a_list() => CronExpression.Parse("1,10-20/5 * * * *").Matches(_monday.AddMinutes(-18)).ShouldBeFalse();
}
