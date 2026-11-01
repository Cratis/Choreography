// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows;

/// <summary>
/// The exception that is thrown when a workflow grain key does not name both a scope and an identity.
/// </summary>
/// <param name="key">The key that could not be read.</param>
public class MalformedWorkflowGrainKey(string key)
    : Exception($"'{key}' is not a workflow grain key - expected '{{scope}}/{{id}}'");
