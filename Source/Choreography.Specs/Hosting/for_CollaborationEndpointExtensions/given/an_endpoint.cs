// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using Cratis.Choreography.Collaboration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Choreography.Hosting.for_CollaborationEndpointExtensions.given;

public class an_endpoint : Specification
{
    protected ICollaborationParticipantResolver _resolver = null!;
    protected ICollaborationHub _hub = null!;
    protected WebSocket _socket = null!;
    protected DefaultHttpContext _context = null!;
    protected WebApplication _app = null!;
    protected RequestDelegate _endpoint = null!;

    protected virtual string QueryString => "?room=room-1&sessionId=session-1&color=ff00ff";

    protected virtual bool IsWebSocketRequest => true;

    void Establish()
    {
        _resolver = Substitute.For<ICollaborationParticipantResolver>();
        _hub = Substitute.For<ICollaborationHub>();
        _socket = Substitute.For<WebSocket>();
        _context = new DefaultHttpContext();
        _context.Request.QueryString = new QueryString(QueryString);

        var webSocketFeature = Substitute.For<IHttpWebSocketFeature>();
        webSocketFeature.IsWebSocketRequest.Returns(IsWebSocketRequest);
        webSocketFeature.AcceptAsync(Arg.Any<WebSocketAcceptContext>()).Returns(_socket);
        _context.Features.Set(webSocketFeature);

        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(_resolver);
        builder.Services.AddSingleton(_hub);
        builder.Services.AddOptions<CollaborationOptions>().Configure(Configure);
        _app = builder.Build();
        _app.MapCratisCollaboration();
        _context.RequestServices = _app.Services;
        _endpoint = ((IEndpointRouteBuilder)_app).DataSources.SelectMany(source => source.Endpoints).Single().RequestDelegate!;
    }

    void Destroy()
    {
        _socket.Dispose();
        _app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    protected virtual void Configure(CollaborationOptions options)
    {
    }
}
