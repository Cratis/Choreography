// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage;

namespace Cratis.Choreography.Patching.for_InMemoryPatchStorage;

public class when_saving_a_patch : Specification
{
    static readonly DateTimeOffset _appliedAt = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
    InMemoryPatchStorage _storage;
    bool _has;
    bool _hasOther;
    List<AppliedPatch> _all;

    void Establish() => _storage = new();

    async Task Because()
    {
        await _storage.Save(new AppliedPatch("a-patch", new SemanticVersion(1, 2, 3), _appliedAt));
        _has = await _storage.Has("a-patch");
        _hasOther = await _storage.Has("another-patch");
        _all = [.. await _storage.GetAll()];
    }

    [Fact] void should_have_the_patch() => _has.ShouldBeTrue();
    [Fact] void should_not_have_another_patch() => _hasOther.ShouldBeFalse();
    [Fact] void should_return_the_patch() => _all.ShouldContainOnly(new AppliedPatch("a-patch", new SemanticVersion(1, 2, 3), _appliedAt));
}
