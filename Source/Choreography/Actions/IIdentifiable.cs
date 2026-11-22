// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Actions;

/// <summary>
/// Defines something that can say which grain it is.
/// </summary>
public interface IIdentifiable
{
    /// <summary>
    /// Gets the <see cref="GrainIdentity"/>.
    /// </summary>
    GrainIdentity Identity { get; }
}
