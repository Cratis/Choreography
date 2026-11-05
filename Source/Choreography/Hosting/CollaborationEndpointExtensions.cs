// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Collaboration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Hosting;

/// <summary>
/// Extension methods for mapping the collaboration WebSocket endpoint.
/// </summary>
public static class CollaborationEndpointExtensions
{
    /// <summary>
    /// Maps the collaboration WebSocket endpoint, on <see cref="CollaborationOptions.Path"/>. It accepts a WebSocket,
    /// asks the <see cref="ICollaborationParticipantResolver"/> who is connecting, and hands the socket to the
    /// <see cref="ICollaborationHub"/>.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to map on.</param>
    /// <returns>The same application for chaining.</returns>
    /// <remarks>
    /// The request needs the query parameters <c language="text">room</c> and <c language="text">sessionId</c>; <c language="text">color</c> is optional. A request that is
    /// not a WebSocket upgrade, or lacks a required parameter, is answered with <c language="text">400 Bad Request</c>, and one the
    /// resolver returns no participant for with <c language="text">401 Unauthorized</c>. The application must have called
    /// <c language="csharp">UseWebSockets()</c>, and should place its authentication middleware before this endpoint.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The configured <see cref="CollaborationOptions.Path"/> is empty.</exception>
    public static WebApplication MapCratisCollaboration(this WebApplication app)
    {
        var path = app.Services.GetRequiredService<IOptions<CollaborationOptions>>().Value.Path;
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException($"{nameof(CollaborationOptions)}.{nameof(CollaborationOptions.Path)} must not be empty.");
        }

        app.Map(path, async context =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var room = context.Request.Query["room"].ToString();
            var sessionId = context.Request.Query["sessionId"].ToString();
            var color = context.Request.Query["color"].ToString();
            if (string.IsNullOrEmpty(room) || string.IsNullOrEmpty(sessionId))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var resolver = context.RequestServices.GetRequiredService<ICollaborationParticipantResolver>();
            var participant = await resolver.Resolve(context, room);
            if (participant is null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var socket = await context.WebSockets.AcceptWebSocketAsync();
            var hub = context.RequestServices.GetRequiredService<ICollaborationHub>();
            await hub.Run(participant.GroupKey, sessionId, participant.UserId, participant.DisplayName, color, socket, context.RequestAborted, participant.HasAvatar);
        });

        return app;
    }
}
