// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Orleans.Workflows.Contracts;

/// <summary>
/// A JSON document a workflow carries - a trigger payload, a node's input or output, a signal's payload.
/// </summary>
/// <param name="Json">The JSON text.</param>
/// <remarks>
/// Workflow data is recorded in the run's journal, so it is kept small: at most
/// <see cref="MaxBytes"/> of UTF-8. Anything larger is stored by the adapter that produced it and
/// referred to from here, the same way evidence is.
/// </remarks>
public record WorkflowData(string Json) : ConceptAs<string>(Json)
{
    /// <summary>
    /// The largest document a workflow records, in UTF-8 bytes.
    /// </summary>
    public const int MaxBytes = 32 * 1024;

    /// <summary>
    /// The empty document.
    /// </summary>
    public static readonly WorkflowData Empty = new("{}");

    /// <summary>
    /// Gets a value indicating whether the document is larger than <see cref="MaxBytes"/>.
    /// </summary>
    public bool ExceedsLimit => Encoding.UTF8.GetByteCount(Value) > MaxBytes;

    /// <summary>
    /// Gets a value indicating whether the document is well-formed JSON.
    /// </summary>
    public bool IsValidJson
    {
        get
        {
            try
            {
                using var document = JsonDocument.Parse(Value);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the document is the empty document.
    /// </summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value) || Value.Trim() == Empty.Value;

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="WorkflowData"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WorkflowData(string value) => new(value);
}
