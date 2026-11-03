// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations;

/// <summary>
/// Exception that gets thrown when an operation kind is defined more than once.
/// </summary>
/// <param name="kind">The kind that already has a definition.</param>
public class OperationAlreadyDefined(OperationKind kind) : Exception($"Operation kind '{kind.Value}' already has a definition");
