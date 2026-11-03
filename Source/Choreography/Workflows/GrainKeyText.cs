// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows;

/// <summary>
/// The text form of the engine's two-part grain keys - <c lang="csharp">"{scope}/{id}"</c>.
/// </summary>
/// <remarks>
/// A key is split at its last <c lang="csharp">/</c>: identities are generated and never contain one, while a scope
/// name is the part that is not under the engine's control.
/// </remarks>
static class GrainKeyText
{
    const char Separator = '/';

    /// <summary>
    /// Joins a scope and an identity into key text.
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <param name="id">The identity.</param>
    /// <returns>The key text.</returns>
    public static string Join(string scope, string id) => $"{scope}{Separator}{id}";

    /// <summary>
    /// Splits key text into its scope and identity.
    /// </summary>
    /// <param name="key">The key text.</param>
    /// <returns>The scope and the identity.</returns>
    /// <exception cref="MalformedWorkflowGrainKey">The text has no separator.</exception>
    public static (string Scope, string Id) Split(string key)
    {
        var separator = key.LastIndexOf(Separator);
        if (separator < 0)
        {
            throw new MalformedWorkflowGrainKey(key);
        }

        return (key[..separator], key[(separator + 1)..]);
    }
}
