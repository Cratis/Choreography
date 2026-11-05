// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Choreography.Collaboration.for_OrleansCollaborationBackplane.given;

public class a_backplane : Specification
{
    protected const string Group = "organization|application";
    protected IGrainFactory _factory = null!;
    protected ICollaborationRoomGrain _grain = null!;
    protected ILocalCollaborationDelivery _delivery = null!;
    protected OrleansCollaborationBackplane _backplane = null!;
    protected Collaborator _collaborator = null!;
    protected Guid _connectionId;
    protected readonly List<TimeSpan> _delays = [];
    protected Result<IReadOnlyList<Collaborator>, CollaborationUnavailable> _result;

    void Establish()
    {
        _factory = Substitute.For<IGrainFactory>();
        _grain = Substitute.For<ICollaborationRoomGrain>();
        _delivery = Substitute.For<ILocalCollaborationDelivery>();
        _factory.GetGrain<ICollaborationRoomGrain>(Group).Returns(_grain);
        _factory.CreateObjectReference<ICollaborationRoomObserver>(Arg.Any<ICollaborationRoomObserver>())
            .Returns(Substitute.For<ICollaborationRoomObserver>());
        _grain.Join(Arg.Any<Collaborator>(), Arg.Any<Guid>(), Arg.Any<Guid>()).Returns([]);
        _collaborator = new("session", Guid.NewGuid().ToString(), "Alice", "fff", 10, 20, "board", "surface", false);
        _connectionId = Guid.NewGuid();
        _backplane = new(_factory, Microsoft.Extensions.Logging.Abstractions.NullLogger<OrleansCollaborationBackplane>.Instance, new CollaborationRetry(delay =>
        {
            _delays.Add(delay);
            return Task.CompletedTask;
        }));
        _backplane.Attach(_delivery);
    }

    void Destroy() => _backplane.Dispose();
}
