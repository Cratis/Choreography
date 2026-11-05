// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Collaboration.for_OrleansCollaborationBackplane.when_leaving;

public class and_the_silo_leaves : given.a_backplane
{
    bool _left;

    void Establish() => _grain.Leave("session", _connectionId, Arg.Any<Guid>())
        .Returns(Task.FromException(new SiloUnavailableException("Silo left")), Task.CompletedTask);

    async Task Because() => _left = await _backplane.Leave(Group, "session", _connectionId);

    [Fact] void should_retry_the_leave() => _left.ShouldBeTrue();
    [Fact] void should_preserve_the_connection_token() => _grain.Received(2).Leave("session", _connectionId, Arg.Any<Guid>());
}
