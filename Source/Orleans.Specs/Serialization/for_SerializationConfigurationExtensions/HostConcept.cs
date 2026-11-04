// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SomeHost;

/// <summary>
/// A concept in a host's own namespace, as applications declare them.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record HostConcept(Guid Value) : ConceptAs<Guid>(Value);
