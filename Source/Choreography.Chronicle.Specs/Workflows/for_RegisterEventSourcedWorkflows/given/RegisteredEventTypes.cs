// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using Cratis.Chronicle.Registrations;

namespace Cratis.Choreography.Chronicle.Workflows.for_RegisterEventSourcedWorkflows.given;

/// <summary>
/// Reads the event types registered on <see cref="ExplicitArtifacts"/>.
/// </summary>
public static class RegisteredEventTypes
{
    /// <summary>
    /// Gets the names of the event types registered on the given artifacts.
    /// </summary>
    /// <param name="artifacts">The <see cref="ExplicitArtifacts"/> to read.</param>
    /// <returns>The names of the registered types.</returns>
    public static IEnumerable<string> Of(ExplicitArtifacts artifacts)
    {
        // The snapshot and its registrations are internal to the client, so they are read by reflection.
        var snapshot = typeof(ExplicitArtifacts).GetMethod("CreateSnapshot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(artifacts, null)!;
        var registrations = (IEnumerable)snapshot.GetType().GetProperty("EventTypes")!.GetValue(snapshot)!;
        return [.. registrations.Cast<object>().Select(registration => ((Type)registration.GetType().GetProperty("Type")!.GetValue(registration)!).Name)];
    }
}
