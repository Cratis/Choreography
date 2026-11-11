// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Tools.GrainCompatibility;

/// <summary>
/// What the tool was asked to do.
/// </summary>
public record Options
{
    /// <summary>
    /// Gets the OrleansContracts.txt of the previous release.
    /// </summary>
    public string? Baseline { get; init; }

    /// <summary>
    /// Gets the OrleansContracts.txt being checked.
    /// </summary>
    public string? Current { get; init; }

    /// <summary>
    /// Gets what to call the baseline in the report, typically its release version.
    /// </summary>
    public string BaselineName { get; init; } = "baseline";

    /// <summary>
    /// Gets whether to emit GitHub workflow commands for each break.
    /// </summary>
    public bool GitHub { get; init; }

    /// <summary>
    /// Gets whether to check the comparison itself against planted breaks instead.
    /// </summary>
    public bool SelfTest { get; init; }

    /// <summary>
    /// How to invoke the tool.
    /// </summary>
    public static string Usage { get; } = string.Join(
        Environment.NewLine,
        "Usage: GrainCompatibility --baseline <OrleansContracts.txt> --current <OrleansContracts.txt> [options]",
        "       GrainCompatibility --self-test",
        string.Empty,
        "  --baseline <path>        The grain contracts of the previous release.",
        "  --current <path>         The grain contracts being checked.",
        "  --baseline-name <name>   What to call the baseline in the report. Defaults to 'baseline'.",
        "  --github                 Emit GitHub workflow commands for each break.",
        "  --self-test              Check that every kind of break is still detected, then exit.",
        string.Empty,
        "Exits 0 when the current contracts serve the baseline, 1 when they do not, 2 when it could not tell.");

    /// <summary>
    /// Parses command line arguments.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns>The parsed <see cref="Options"/>.</returns>
    /// <exception cref="InvalidArguments">Thrown when the arguments do not describe something the tool can do.</exception>
    public static Options Parse(string[] args)
    {
        var options = new Options();
        for (var index = 0; index < args.Length; index++)
        {
            options = options.Apply(args, ref index);
        }

        if (options.SelfTest)
        {
            return options with { Baseline = null, Current = null };
        }

        if (options.Baseline is null || options.Current is null)
        {
            throw new InvalidArguments("Both --baseline and --current are required.");
        }

        return options;
    }

    Options Apply(string[] args, ref int index) => args[index] switch
    {
        "--baseline" => this with { Baseline = ValueOf(args, ref index) },
        "--current" => this with { Current = ValueOf(args, ref index) },
        "--baseline-name" => this with { BaselineName = ValueOf(args, ref index) },
        "--github" => this with { GitHub = true },
        "--self-test" => this with { SelfTest = true },
        _ => throw new InvalidArguments($"Unknown argument '{args[index]}'.")
    };

    static string ValueOf(string[] args, ref int index)
    {
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            throw new InvalidArguments($"'{args[index]}' needs a value.");
        }

        return args[++index];
    }
}
