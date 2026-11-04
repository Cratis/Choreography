// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Choreography.Patching;

/// <summary>
/// Provides the version of the running host - the executable the silo is part of, not this library.
/// </summary>
internal static class HostVersion
{
    /// <summary>
    /// Gets the informational version of the running host, including build metadata.
    /// </summary>
    internal static readonly string InformationalVersion = ReadInformationalVersion();

    /// <summary>
    /// Gets the running host's version without build metadata.
    /// </summary>
    internal static SemanticVersion Current { get; } = Parse(InformationalVersion);

    /// <summary>
    /// Parses an informational version into a semantic version, ignoring build metadata.
    /// </summary>
    /// <param name="informationalVersion">The informational version to parse.</param>
    /// <returns>The parsed version, or <see cref="SemanticVersion.NotSet"/> when it cannot be parsed.</returns>
    internal static SemanticVersion Parse(string informationalVersion)
    {
        var plusIndex = informationalVersion.IndexOf('+', StringComparison.Ordinal);
        var withoutMetadata = plusIndex > 0 ? informationalVersion[..plusIndex] : informationalVersion;
        return SemanticVersion.TryParse(withoutMetadata, out var version) ? version! : SemanticVersion.NotSet;
    }

    /// <summary>
    /// Determines whether a version identifies a released host rather than a development placeholder.
    /// </summary>
    /// <param name="version">The version to check.</param>
    /// <returns>True when the version is neither unset nor the 1.0.0 placeholder an unversioned build carries.</returns>
    internal static bool IsKnown(SemanticVersion version) =>
        version.CompareTo(SemanticVersion.NotSet) != 0 && version.CompareTo(new SemanticVersion(1, 0, 0)) != 0;

    static string ReadInformationalVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? typeof(HostVersion).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "0.0.0";
    }
}
