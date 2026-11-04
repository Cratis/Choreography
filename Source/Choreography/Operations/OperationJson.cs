// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Choreography.Operations;

/// <summary>
/// The serializer settings used for the intent and memento stored with an operation.
/// </summary>
static class OperationJson
{
    /// <summary>
    /// Gets the options.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
