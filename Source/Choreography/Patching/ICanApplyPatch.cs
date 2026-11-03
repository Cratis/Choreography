// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching;

/// <summary>
/// Defines a patch - a one-time change to the data a host owns, applied when the host starts on a version that carries it.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface and the patch is discovered; nothing has to be registered. A patch is created with the container
/// supplying its dependencies, so it can ask for the storage, services and loggers it needs.
/// </para>
/// <para>
/// A patch runs once per store. The patch manager records it as applied only after <see cref="Up"/> completes, so a patch that
/// fails is attempted again on the next start and must be safe to run more than once.
/// </para>
/// </remarks>
public interface ICanApplyPatch
{
    /// <summary>
    /// Gets the version the patch belongs to - the release that introduced the change it makes.
    /// </summary>
    /// <remarks>
    /// Patches run in ascending order of this version, and a patch whose version is not newer than the version already patched
    /// is never applied.
    /// </remarks>
    SemanticVersion Version { get; }

    /// <summary>
    /// Gets the name of the patch, which identifies it in the record of applied patches. Defaults to the name of the type.
    /// </summary>
    /// <remarks>
    /// Renaming a patch makes it a different patch and so applies it again.
    /// </remarks>
    string Name => GetType().Name;

    /// <summary>
    /// Applies the patch.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Up();
}
