// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;

namespace Cratis.Choreography.Collaboration;

public partial class CollaborationHub
{
    sealed class Connection(WebSocket socket, Collaborator state) : IDisposable
    {
        readonly SemaphoreSlim _sendLock = new(1, 1);

        public WebSocket Socket => socket;

        public Collaborator State { get; set; } = state;

        public DateTimeOffset LastActive { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset LastReceived { get; set; } = DateTimeOffset.UtcNow;

        public bool Joined { get; set; }

        public void Dispose() => _sendLock.Dispose();

        public async Task SendRaw(byte[] payload)
        {
            if (Socket.State != WebSocketState.Open)
            {
                return;
            }

            await _sendLock.WaitAsync();
            try
            {
                await Socket.SendAsync(payload, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
            }
            catch (WebSocketException)
            {
                // dead socket - the receive loop notices and leaves
            }
            catch (ObjectDisposedException)
            {
                // socket torn down concurrently with a broadcast - same outcome as a dead socket
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public async Task Reconnect()
        {
            try
            {
                if (Socket.State == WebSocketState.Open)
                {
                    await Socket.CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable, "Collaboration is reconnecting", CancellationToken.None);
                }
            }
            catch (WebSocketException)
            {
                // The client already disconnected and will use its normal reconnect path.
            }
            finally
            {
                Socket.Abort();
            }
        }

        public async Task CloseSilently()
        {
            try
            {
                if (Socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                {
                    await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", CancellationToken.None);
                }
            }
            catch (WebSocketException)
            {
                // already gone
            }
            catch (ObjectDisposedException)
            {
                // already gone
            }
        }
    }
}
