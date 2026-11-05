// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Collaboration;

/// <summary>
/// Options for the collaboration WebSocket endpoint.
/// </summary>
public class CollaborationOptions
{
    /// <summary>
    /// The default value of <see cref="Path"/>.
    /// </summary>
    public const string DefaultPath = "/ws/collaboration";

    /// <summary>
    /// Gets or sets the path the WebSocket endpoint is mapped on. Defaults to <c language="text">/ws/collaboration</c>.
    /// </summary>
    /// <remarks>
    /// The JavaScript client connects to the same path, so change it on both sides.
    /// </remarks>
    public string Path { get; set; } = DefaultPath;
}
