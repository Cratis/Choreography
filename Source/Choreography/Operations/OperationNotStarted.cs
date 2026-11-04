// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations;

/// <summary>
/// Exception that gets thrown when something is recorded on an operation that already finished.
/// </summary>
/// <param name="id">The operation.</param>
/// <param name="status">Its current status.</param>
public class OperationNotStarted(OperationId id, OperationStatus status)
    : Exception($"Operation {id.Value} is {status} and can no longer be changed");
