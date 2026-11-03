// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage;

namespace Cratis.Choreography.Patching.for_InMemoryPatchStorage;

public class when_saving_a_patch_that_is_already_recorded : Specification
{
    static readonly DateTimeOffset _later = new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);
    InMemoryPatchStorage _storage;
    List<AppliedPatch> _all;

    void Establish() => _storage = new();

    async Task Because()
    {
        await _storage.Save(new AppliedPatch("a-patch", new SemanticVersion(1, 2, 3), _later.AddDays(-1)));
        await _storage.Save(new AppliedPatch("a-patch", new SemanticVersion(1, 2, 3), _later));
        _all = [.. await _storage.GetAll()];
    }

    [Fact] void should_keep_one_record() => _all.Count.ShouldEqual(1);
    [Fact] void should_keep_the_latest_record() => _all[0].AppliedAt.ShouldEqual(_later);
}
