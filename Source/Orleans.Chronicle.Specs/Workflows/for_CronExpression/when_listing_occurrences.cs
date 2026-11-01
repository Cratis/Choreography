// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Chronicle.Workflows.for_CronExpression;

public class when_listing_occurrences : Specification
{
    List<DateTimeOffset> _occurrences = [];

    void Because() => _occurrences = [.. CronExpression.Parse("*/10 * * * *").OccurrencesBetween(new DateTimeOffset(2026, 3, 2, 9, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 2, 9, 30, 30, TimeSpan.Zero))];

    [Fact] void should_exclude_the_start() => _occurrences.ShouldNotContain(new DateTimeOffset(2026, 3, 2, 9, 0, 0, TimeSpan.Zero));
    [Fact] void should_include_every_match_up_to_the_end() => _occurrences.ShouldEqual([new DateTimeOffset(2026, 3, 2, 9, 10, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 2, 9, 20, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 2, 9, 30, 0, TimeSpan.Zero)]);
}
