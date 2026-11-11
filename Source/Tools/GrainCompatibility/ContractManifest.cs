// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;

namespace Cratis.Choreography.Tools.GrainCompatibility;

/// <summary>
/// The grain contracts of one build, read from the OrleansContracts.txt the Orleans contract analyzer maintains.
/// </summary>
/// <param name="Interfaces">The grain interfaces, by the grain interface type Orleans identifies them by.</param>
/// <param name="Classes">The grain classes, by the grain type Orleans identifies them by.</param>
public partial record ContractManifest(
    IReadOnlyDictionary<string, GrainInterfaceContract> Interfaces,
    IReadOnlyDictionary<string, GrainClassContract> Classes)
{
    const string RetiredPrefix = "*RETIRED* ";
    const string MethodIndent = "  ";
    const int MatchTimeoutMilliseconds = 1000;

    /// <summary>
    /// Gets the active interfaces, leaving out the retired ones kept as history.
    /// </summary>
    public IEnumerable<GrainInterfaceContract> ActiveInterfaces => Interfaces.Values.Where(_ => !_.Retired);

    /// <summary>
    /// Gets the active grain classes, leaving out the retired ones kept as history.
    /// </summary>
    public IEnumerable<GrainClassContract> ActiveClasses => Classes.Values.Where(_ => !_.Retired);

    [GeneratedRegex("""^interface \[GrainInterfaceType\("(?<type>(?:[^"\\]|\\.)*)"\)\] (?<name>.+) \[Version\((?<version>\d+)\)\]$""", RegexOptions.None, MatchTimeoutMilliseconds)]
    private static partial Regex InterfaceLine { get; }

    [GeneratedRegex("""^class \[GrainType\("(?<type>(?:[^"\\]|\\.)*)"\)\] (?<name>.+)$""", RegexOptions.None, MatchTimeoutMilliseconds)]
    private static partial Regex ClassLine { get; }

    [GeneratedRegex(@"\\(?<character>.)", RegexOptions.None, MatchTimeoutMilliseconds)]
    private static partial Regex EscapedCharacter { get; }

    /// <summary>
    /// Reads a manifest from its text.
    /// </summary>
    /// <param name="text">The content of an OrleansContracts.txt.</param>
    /// <returns>The <see cref="ContractManifest"/>.</returns>
    /// <exception cref="InvalidContractManifest">Thrown when a line cannot be read.</exception>
    public static ContractManifest Parse(string text)
    {
        var reader = new ManifestReader();
        var lines = text.ReplaceLineEndings("\n").Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            reader.ReadLine(index + 1, lines[index].TrimEnd());
        }

        return new ContractManifest(reader.Interfaces, reader.Classes);
    }

    static GrainMethodContract ParseMethod(int lineNumber, string line)
    {
        var separator = IndexOfUnescapedSeparator(line);
        if (separator <= 0)
        {
            throw new InvalidContractManifest(lineNumber, $"A method line has no wire identity: '{line}'.");
        }

        var identity = Unescape(line[..separator]);
        var rest = line[(separator + 2)..];
        var parenthesis = rest.IndexOf('(', StringComparison.Ordinal);
        if (parenthesis <= 0)
        {
            throw new InvalidContractManifest(lineNumber, $"A method line has no parameter list: '{line}'.");
        }

        return new GrainMethodContract(identity, rest[..parenthesis], rest[parenthesis..]);
    }

    static int IndexOfUnescapedSeparator(string line)
    {
        for (var index = 0; index < line.Length - 1; index++)
        {
            if (line[index] == '\\')
            {
                index++;
                continue;
            }

            if (line[index] == ':' && line[index + 1] == ' ')
            {
                return index;
            }
        }

        return -1;
    }

    static string Unescape(string value) => EscapedCharacter.Replace(value, "${character}");

    sealed class ManifestReader
    {
        int _lineNumber;
        bool _retired;
        string? _currentInterface;
        Dictionary<string, GrainMethodContract>? _currentMethods;

        public Dictionary<string, GrainInterfaceContract> Interfaces { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, GrainClassContract> Classes { get; } = new(StringComparer.Ordinal);

        public void ReadLine(int lineNumber, string line)
        {
            _lineNumber = lineNumber;
            if (line.Length == 0 || line.StartsWith('#'))
            {
                return;
            }

            if (line.StartsWith(MethodIndent, StringComparison.Ordinal))
            {
                ReadMethod(line);
                return;
            }

            _retired = line.StartsWith(RetiredPrefix, StringComparison.Ordinal);
            var declaration = _retired ? line[RetiredPrefix.Length..] : line;

            if (InterfaceLine.Match(declaration) is { Success: true } interfaceMatch)
            {
                ReadInterface(interfaceMatch);
            }
            else if (ClassLine.Match(declaration) is { Success: true } classMatch)
            {
                ReadClass(classMatch);
            }
            else
            {
                throw new InvalidContractManifest(lineNumber, $"Not an interface, class or method declaration: '{line}'.");
            }
        }

        void ReadMethod(string line)
        {
            if (_currentMethods is null)
            {
                throw new InvalidContractManifest(_lineNumber, "A method appears before any interface.");
            }

            var method = ParseMethod(_lineNumber, line[MethodIndent.Length..]);
            if (!_currentMethods.TryAdd(method.Identity, method))
            {
                throw new InvalidContractManifest(_lineNumber, $"Method identity '{method.Identity}' appears twice on {_currentInterface}.");
            }
        }

        void ReadInterface(Match match)
        {
            var interfaceType = Unescape(match.Groups["type"].Value);
            _currentMethods = [with(StringComparer.Ordinal)];
            var contract = new GrainInterfaceContract(
                interfaceType,
                match.Groups["name"].Value,
                int.Parse(match.Groups["version"].Value, CultureInfo.InvariantCulture),
                _retired,
                _currentMethods);

            if (!Interfaces.TryAdd(interfaceType, contract))
            {
                throw new InvalidContractManifest(_lineNumber, $"Interface '{interfaceType}' is declared twice.");
            }

            _currentInterface = interfaceType;
        }

        void ReadClass(Match match)
        {
            var grainType = Unescape(match.Groups["type"].Value);
            if (!Classes.TryAdd(grainType, new GrainClassContract(grainType, match.Groups["name"].Value, _retired)))
            {
                throw new InvalidContractManifest(_lineNumber, $"Grain class '{grainType}' is declared twice.");
            }

            _currentInterface = null;
            _currentMethods = null;
        }
    }
}
