// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace SomeHost;

/// <summary>
/// A comparable type outside the Cratis namespaces, usable as a concept value, that nothing registered for these specs allows.
/// </summary>
[SuppressMessage("Design", "CA1036", Justification = "Only its type is used, never its comparisons.")]
public class ForeignComparable : IComparable
{
    /// <inheritdoc/>
    public int CompareTo(object? obj) => 0;
}
