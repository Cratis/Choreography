// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using System.Text;

namespace Cratis.Choreography.Collaboration.for_CollaborationHub.given;

public class a_socket : WebSocket
{
    WebSocketState _state = WebSocketState.Open;
    public readonly Queue<string> Incoming = new();
    public readonly List<string> Sent = [];
    public WebSocketCloseStatus? RequestedClose { get; set; }
    public bool Aborted { get; set; }
    public override WebSocketCloseStatus? CloseStatus => RequestedClose;
    public override string? CloseStatusDescription => null;
    public override string? SubProtocol => null;
    public override WebSocketState State => _state;

    public override void Abort()
    {
        Aborted = true;
        _state = WebSocketState.Aborted;
    }

    public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
    {
        RequestedClose = closeStatus;
        _state = WebSocketState.Closed;
        return Task.CompletedTask;
    }

    public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
    {
        RequestedClose = closeStatus;
        _state = WebSocketState.CloseSent;
        return Task.CompletedTask;
    }

    public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
    {
        if (Incoming.TryDequeue(out var message))
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            bytes.AsSpan().CopyTo(buffer.AsSpan());
            return Task.FromResult(new WebSocketReceiveResult(bytes.Length, System.Net.WebSockets.WebSocketMessageType.Text, true));
        }

        return Task.FromResult(new WebSocketReceiveResult(0, System.Net.WebSockets.WebSocketMessageType.Close, true));
    }

    public override Task SendAsync(ArraySegment<byte> buffer, System.Net.WebSockets.WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
    {
        Sent.Add(Encoding.UTF8.GetString(buffer));
        return Task.CompletedTask;
    }

    public override void Dispose() => _state = WebSocketState.Closed;
}
