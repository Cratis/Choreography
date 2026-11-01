// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Journal;

/// <summary>
/// The exception that is thrown when a journal holds a fact no run records.
/// </summary>
/// <param name="name">The name of the fact's type.</param>
public class UnknownWorkflowFact(string name)
    : Exception($"'{name}' is not a fact a workflow run records");
