// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Serialization.for_SerializationConfigurationExtensions.given;

/// <summary>
/// A generic record in a Cratis namespace.
/// </summary>
/// <param name="Value">The content.</param>
/// <typeparam name="T">Type of the content.</typeparam>
public record CratisGeneric<T>(T Value);
