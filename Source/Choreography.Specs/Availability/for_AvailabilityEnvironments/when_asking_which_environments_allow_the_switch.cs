// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Availability.for_AvailabilityEnvironments;

public class when_asking_which_environments_allow_the_switch : Specification
{
    [Theory]
    [InlineData("Development", true)]
    [InlineData("local", true)]
    [InlineData("Test", true)]
    [InlineData("Testing", true)]
    [InlineData("Production", false)]
    [InlineData("Staging", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    void should_allow_only_local_and_test(string? name, bool expected) => AvailabilityEnvironments.AllowsGrantAll(name).ShouldEqual(expected);
}
