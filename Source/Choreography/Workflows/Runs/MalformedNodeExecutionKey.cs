// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs;

/// <summary>
/// The exception that is thrown when text does not describe a node execution.
/// </summary>
/// <param name="text">The text that could not be read.</param>
public class MalformedNodeExecutionKey(string text)
    : Exception($"'{text}' is not a node execution key - expected '{{node}}/{{branch}}/{{visit}}'");
