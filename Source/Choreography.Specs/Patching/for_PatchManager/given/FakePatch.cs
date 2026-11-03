// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchManager.given;

/// <summary>
/// A patch that counts how often it was applied and runs whatever its spec tells it to.
/// </summary>
/// <param name="name">The name of the patch.</param>
/// <param name="version">The version the patch belongs to.</param>
public class FakePatch(string name, SemanticVersion version) : ICanApplyPatch
{
    public string Name => name;

    public SemanticVersion Version => version;

    public int UpCalls { get; private set; }

    public Func<Task> Run { get; set; } = () => Task.CompletedTask;

    public Task Up()
    {
        UpCalls++;
        return Run();
    }
}
