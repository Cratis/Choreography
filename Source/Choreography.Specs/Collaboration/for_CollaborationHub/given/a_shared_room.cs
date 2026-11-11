// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Choreography.Collaboration.for_CollaborationHub.given;

public class a_shared_room : Specification
{
    protected readonly CollaborationRoster _roster = new();
    protected readonly List<Collaborator> _joins = [];
    protected readonly List<string> _leaves = [];
    protected CollaborationHub _hub = null!;
    protected CollaborationHub _otherHub = null!;
    readonly List<ICollaborationRoomObserver> _observers = [];
    readonly List<OrleansCollaborationBackplane> _backplanes = [];
    readonly List<a_blocking_socket> _sockets = [];
    protected readonly List<Task> _runs = [];
    ICollaborationRoomGrain _grain = null!;

    void Establish()
    {
        _grain = Substitute.For<ICollaborationRoomGrain>();
        _grain.Subscribe(Arg.Any<ICollaborationRoomObserver>()).Returns(call =>
        {
            _observers.Add(call.Arg<ICollaborationRoomObserver>());
            return Task.CompletedTask;
        });
        _grain.Join(Arg.Any<Collaborator>(), Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(call =>
        {
            var collaborator = call.Arg<Collaborator>();
            var pod = call.ArgAt<Guid>(2);
            _joins.Add(collaborator);
            var others = _roster.Join(collaborator, call.ArgAt<Guid>(1), pod);
            foreach (var observer in _observers)
            {
                observer.OnJoined("group", collaborator, pod);
            }

            return Task.FromResult(others);
        });
        _grain.Leave(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(call =>
        {
            var session = call.Arg<string>();
            if (_roster.Leave(session, call.ArgAt<Guid>(1)))
            {
                _leaves.Add(session);
            }

            return Task.CompletedTask;
        });
        _grain.Presence(Arg.Any<string>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>()).Returns(call =>
        {
            _roster.Presence(call.ArgAt<string>(0), call.ArgAt<double>(1), call.ArgAt<double>(2), call.ArgAt<string>(3), call.ArgAt<string>(4));
            return Task.CompletedTask;
        });
        _hub = CreateHub();
        _otherHub = CreateHub();
    }

    protected a_blocking_socket Connect(CollaborationHub hub, string user, string session = "session", string group = "group")
    {
        var socket = new a_blocking_socket();
        _sockets.Add(socket);
        _runs.Add(hub.Run(group, session, user, user, "fff", socket, CancellationToken.None));
        return socket;
    }

    async Task Destroy()
    {
        foreach (var socket in _sockets)
        {
            socket.Abort();
        }

        await Task.WhenAll(_runs).WaitAsync(TimeSpan.FromSeconds(10));
        foreach (var backplane in _backplanes)
        {
            backplane.Dispose();
        }
    }

    CollaborationHub CreateHub()
    {
        var factory = Substitute.For<IGrainFactory>();
        factory.GetGrain<ICollaborationRoomGrain>(Arg.Any<string>()).Returns(_grain);
        factory.CreateObjectReference<ICollaborationRoomObserver>(Arg.Any<ICollaborationRoomObserver>())
            .Returns(call => call.Arg<ICollaborationRoomObserver>());
        var backplane = new OrleansCollaborationBackplane(factory, NullLogger<OrleansCollaborationBackplane>.Instance);
        _backplanes.Add(backplane);
        return new(backplane);
    }
}
