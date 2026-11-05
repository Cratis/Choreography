// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;

namespace Cratis.Choreography.Collaboration.for_CollaborationHub.given;

/// <summary>
/// A socket that stays open and silent until it is aborted, like a connected browser tab.
/// </summary>
public class a_blocking_socket : a_socket
{
    readonly TaskCompletionSource _aborted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public override void Abort()
    {
        base.Abort();
        _aborted.TrySetResult();
    }

    public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
    {
        await _aborted.Task.WaitAsync(cancellationToken);
        throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
    }
}
