// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations;

/// <summary>
/// Defines the handle a unit of work uses to associate the events it produces with the operation being recorded.
/// </summary>
public interface IOperationRecording
{
    /// <summary>
    /// Gets the identity of the operation being recorded. Hosts can stamp it on the events they append.
    /// </summary>
    OperationId Id { get; }

    /// <summary>
    /// Associate event sequence numbers with the operation. Call as often as needed; zero calls records an
    /// operation without events.
    /// </summary>
    /// <param name="eventSequenceNumbers">The sequence numbers of the events produced.</param>
    /// <returns>Awaitable task.</returns>
    Task Associate(IEnumerable<ulong> eventSequenceNumbers);
}
