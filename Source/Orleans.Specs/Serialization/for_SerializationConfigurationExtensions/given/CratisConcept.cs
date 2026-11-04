// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Serialization.for_SerializationConfigurationExtensions.given;

/// <summary>
/// A concept in a Cratis namespace.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record CratisConcept(Guid Value) : ConceptAs<Guid>(Value);
