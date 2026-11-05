// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Collaboration;

/// <summary>
/// The error of a backplane operation that could not be completed because the cluster stayed unavailable for the
/// whole retry budget. The affected socket is asked to reconnect, which re-establishes its presence.
/// </summary>
public record CollaborationUnavailable;
