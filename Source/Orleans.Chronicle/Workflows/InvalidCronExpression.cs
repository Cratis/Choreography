// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Chronicle.Workflows;

/// <summary>
/// The exception that is thrown when a cron expression cannot be read.
/// </summary>
/// <param name="text">The text that could not be read.</param>
/// <param name="problem">What is wrong with it.</param>
public class InvalidCronExpression(string text, string problem)
    : Exception($"'{text}' is not a valid cron expression: {problem}");
