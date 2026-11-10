// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Choreography.Patching;

/// <summary>
/// Reads the textual form of a <see cref="SemanticVersion"/>.
/// </summary>
static class SemanticVersionParser
{
    static readonly SuffixRule _buildMetadata = new('+', IsBuildIdentifier);
    static readonly SuffixRule _preRelease = new('-', IsPreReleaseIdentifier);

    /// <summary>
    /// Try to read a semantic version.
    /// </summary>
    /// <param name="version">Version string to read.</param>
    /// <param name="result">The version read, or <see cref="SemanticVersion.NotSet"/> when the string is invalid.</param>
    /// <returns>True if the string is a valid version, false otherwise.</returns>
    internal static bool TryRead(string version, out SemanticVersion result)
    {
        result = SemanticVersion.NotSet;

        if (!TrySplitSuffix(version, _buildMetadata, out var withoutBuild, out var buildMetadata))
        {
            return false;
        }

        if (!TrySplitSuffix(withoutBuild, _preRelease, out var core, out var preRelease))
        {
            return false;
        }

        if (!TryReadCore(core, out var numbers))
        {
            return false;
        }

        result = new SemanticVersion(numbers.Major, numbers.Minor, numbers.Patch, preRelease, buildMetadata);
        return true;
    }

    static bool TrySplitSuffix(string value, SuffixRule rule, out string rest, out string suffix)
    {
        var index = value.IndexOf(rule.Separator, StringComparison.Ordinal);
        if (index < 0)
        {
            rest = value;
            suffix = string.Empty;
            return true;
        }

        suffix = value[(index + 1)..];
        rest = value[..index];
        return suffix.Split('.').All(rule.IsValid);
    }

    static bool TryReadCore(string core, out CoreVersion numbers)
    {
        numbers = null!;
        var parts = core.Split('.');
        if (parts.Length != 3 || !parts.All(IsNumeric))
        {
            return false;
        }

        var values = new int[3];
        for (var index = 0; index < values.Length; index++)
        {
            if (!int.TryParse(parts[index], NumberStyles.None, CultureInfo.InvariantCulture, out values[index]))
            {
                return false;
            }
        }

        numbers = new CoreVersion(values[0], values[1], values[2]);
        return true;
    }

    static bool IsNumeric(string value) => value.Length > 0 && value.All(char.IsAsciiDigit) && (value.Length == 1 || value[0] != '0');

    static bool IsBuildIdentifier(string value) => value.Length > 0 && value.All(character => char.IsAsciiLetterOrDigit(character) || character == '-');

    static bool IsPreReleaseIdentifier(string value) => IsNumeric(value) || (IsBuildIdentifier(value) && !value.All(char.IsAsciiDigit));

    sealed record SuffixRule(char Separator, Func<string, bool> IsValid);

    sealed record CoreVersion(int Major, int Minor, int Patch);
}
