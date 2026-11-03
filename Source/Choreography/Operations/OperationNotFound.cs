// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations;

/// <summary>
/// Exception that gets thrown when an operation is not in the history.
/// </summary>
/// <param name="id">The operation that was not found.</param>
public class OperationNotFound(OperationId id) : Exception($"Operation {id.Value} does not exist in the operation history");
