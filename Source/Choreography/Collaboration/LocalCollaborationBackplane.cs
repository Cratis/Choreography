// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Choreography.Collaboration;

/// <summary>
/// The single-replica backplane: there are no other replicas, so every cross-replica operation is a no-op and the
/// join snapshot has no remote members. This is the default; <c language="csharp">AddCratisCollaborationOrleans</c> replaces it, so
/// with one replica the hub behaves as a pure in-memory instance.
/// </summary>
public class LocalCollaborationBackplane : ICollaborationBackplane
{
    /// <inheritdoc/>
    public void Attach(ILocalCollaborationDelivery delivery)
    {
    }

    /// <inheritdoc/>
    public Task<Result<IReadOnlyList<Collaborator>, CollaborationUnavailable>> Join(string groupKey, Collaborator collaborator, Guid connectionId) =>
        Task.FromResult<Result<IReadOnlyList<Collaborator>, CollaborationUnavailable>>(Array.Empty<Collaborator>());

    /// <inheritdoc/>
    public Task<bool> Leave(string groupKey, string sessionId, Guid connectionId) => Task.FromResult(true);

    /// <inheritdoc/>
    public Task<bool> Presence(string groupKey, string sessionId, double x, double y, string area, string surfaceId) => Task.FromResult(true);

    /// <inheritdoc/>
    public Task<bool> Idle(string groupKey, string sessionId, bool isIdle) => Task.FromResult(true);

    /// <inheritdoc/>
    public Task Release(string groupKey) => Task.CompletedTask;
}
