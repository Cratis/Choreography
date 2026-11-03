// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Choreography.Patching;

/// <summary>
/// Represents a semantic version.
/// </summary>
/// <param name="Major">Major version number.</param>
/// <param name="Minor">Minor version number.</param>
/// <param name="Patch">Patch version number.</param>
/// <param name="PreRelease">Optional pre-release identifier.</param>
/// <param name="BuildMetadata">Optional build metadata.</param>
public record SemanticVersion(int Major, int Minor, int Patch, string PreRelease = "", string BuildMetadata = "") : IComparable<SemanticVersion>, IComparable
{
    /// <summary>
    /// Gets an empty version (0.0.0).
    /// </summary>
    public static readonly SemanticVersion NotSet = new(0, 0, 0);

    /// <summary>
    /// Implicit conversion from string to <see cref="SemanticVersion"/>.
    /// </summary>
    /// <param name="version">Version string.</param>
    public static implicit operator SemanticVersion(string version) => Parse(version);

    /// <summary>
    /// Greater than operator.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>True if left is greater than right.</returns>
    public static bool operator >(SemanticVersion left, SemanticVersion right) =>
        left.CompareTo(right) > 0;

    /// <summary>
    /// Less than operator.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>True if left is less than right.</returns>
    public static bool operator <(SemanticVersion left, SemanticVersion right) =>
        left.CompareTo(right) < 0;

    /// <summary>
    /// Greater than or equal operator.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>True if left is greater than or equal to right.</returns>
    public static bool operator >=(SemanticVersion left, SemanticVersion right) =>
        left.CompareTo(right) >= 0;

    /// <summary>
    /// Less than or equal operator.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>True if left is less than or equal to right.</returns>
    public static bool operator <=(SemanticVersion left, SemanticVersion right) =>
        left.CompareTo(right) <= 0;

    /// <summary>
    /// Parse a semantic version string.
    /// </summary>
    /// <param name="version">Version string to parse.</param>
    /// <returns>Parsed <see cref="SemanticVersion"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if the version string is invalid.</exception>
    public static SemanticVersion Parse(string version)
    {
        if (!TryReadVersion(version, out var parsed))
        {
            throw new ArgumentException($"Invalid semantic version: {version}", nameof(version));
        }

        return parsed;
    }

    /// <summary>
    /// Try to parse a semantic version string.
    /// </summary>
    /// <param name="version">Version string to parse.</param>
    /// <param name="result">The parsed <see cref="SemanticVersion"/> if successful.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    public static bool TryParse(string version, out SemanticVersion? result)
    {
        try
        {
            result = Parse(version);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    /// <inheritdoc/>
    public int CompareTo(SemanticVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        var majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0)
        {
            return majorComparison;
        }

        var minorComparison = Minor.CompareTo(other.Minor);
        if (minorComparison != 0)
        {
            return minorComparison;
        }

        var patchComparison = Patch.CompareTo(other.Patch);
        if (patchComparison != 0)
        {
            return patchComparison;
        }

        if (string.IsNullOrEmpty(PreRelease) && !string.IsNullOrEmpty(other.PreRelease))
        {
            return 1;
        }

        if (!string.IsNullOrEmpty(PreRelease) && string.IsNullOrEmpty(other.PreRelease))
        {
            return -1;
        }

        return ComparePreRelease(PreRelease, other.PreRelease);
    }

    /// <inheritdoc/>
    public int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        if (obj is SemanticVersion other)
        {
            return CompareTo(other);
        }

        throw new ArgumentException($"Object must be of type {nameof(SemanticVersion)}");
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var version = $"{Major}.{Minor}.{Patch}";
        if (!string.IsNullOrEmpty(PreRelease))
        {
            version += $"-{PreRelease}";
        }

        if (!string.IsNullOrEmpty(BuildMetadata))
        {
            version += $"+{BuildMetadata}";
        }

        return version;
    }

    static int ComparePreRelease(string left, string right)
    {
        var leftIdentifiers = left.Split('.');
        var rightIdentifiers = right.Split('.');
        for (var index = 0; index < Math.Min(leftIdentifiers.Length, rightIdentifiers.Length); index++)
        {
            var comparison = CompareIdentifier(leftIdentifiers[index], rightIdentifiers[index]);
            if (comparison != 0) return comparison;
        }

        return leftIdentifiers.Length.CompareTo(rightIdentifiers.Length);
    }

    static int CompareIdentifier(string left, string right)
    {
        var leftIsNumeric = left.Length > 0 && left.All(char.IsAsciiDigit);
        var rightIsNumeric = right.Length > 0 && right.All(char.IsAsciiDigit);
        if (leftIsNumeric != rightIsNumeric) return leftIsNumeric ? -1 : 1;

        // Valid numeric identifiers have no leading zeroes. Comparing lengths first supports
        // arbitrarily large identifiers without overflowing an integer.
        if (leftIsNumeric && left.Length != right.Length) return left.Length.CompareTo(right.Length);

        return string.CompareOrdinal(left, right);
    }

    static bool TryReadVersion(string version, out SemanticVersion result)
    {
        result = NotSet;

        var rest = version;
        var buildMetadata = string.Empty;
        var plus = rest.IndexOf('+', StringComparison.Ordinal);
        if (plus >= 0)
        {
            buildMetadata = rest[(plus + 1)..];
            rest = rest[..plus];
            if (!AllIdentifiers(buildMetadata, IsBuildIdentifier))
            {
                return false;
            }
        }

        var preRelease = string.Empty;
        var dash = rest.IndexOf('-', StringComparison.Ordinal);
        if (dash >= 0)
        {
            preRelease = rest[(dash + 1)..];
            rest = rest[..dash];
            if (!AllIdentifiers(preRelease, IsPreReleaseIdentifier))
            {
                return false;
            }
        }

        var numbers = rest.Split('.');
        if (numbers.Length != 3 || !numbers.All(IsNumeric))
        {
            return false;
        }

        if (!int.TryParse(numbers[0], NumberStyles.None, CultureInfo.InvariantCulture, out var major) ||
            !int.TryParse(numbers[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minor) ||
            !int.TryParse(numbers[2], NumberStyles.None, CultureInfo.InvariantCulture, out var patch))
        {
            return false;
        }

        result = new SemanticVersion(major, minor, patch, preRelease, buildMetadata);
        return true;
    }

    static bool AllIdentifiers(string identifiers, Func<string, bool> isValid) => identifiers.Split('.').All(isValid);

    static bool IsNumeric(string value) => value.Length > 0 && value.All(char.IsAsciiDigit) && (value.Length == 1 || value[0] != '0');

    static bool IsBuildIdentifier(string value) => value.Length > 0 && value.All(character => char.IsAsciiLetterOrDigit(character) || character == '-');

    static bool IsPreReleaseIdentifier(string value) => IsNumeric(value) || (value.Length > 0 && IsBuildIdentifier(value) && !value.All(char.IsAsciiDigit));
}
