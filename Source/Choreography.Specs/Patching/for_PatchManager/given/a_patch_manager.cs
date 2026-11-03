// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.Patching;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.TestKit;

namespace Cratis.Choreography.Patching.for_PatchManager.given;

public class a_patch_manager : Specification
{
    protected TestKitSilo _silo = new();
    protected IPatchStorage _storage;
    protected FakePatch _patch;
    protected PatchManager _manager;
    protected SemanticVersion _version = new(19, 100, 0);
    protected List<ICanApplyPatch> _patches = [];

    void Establish()
    {
        _storage = Substitute.For<IPatchStorage>();
        _patch = APatch("a-patch", new SemanticVersion(19, 90, 0));
        _patches.Add(_patch);
        var patches = Substitute.For<IInstancesOf<ICanApplyPatch>>();
        patches.GetEnumerator().Returns(_ => _patches.GetEnumerator());
        _silo.AddService(_storage);
        _silo.AddService(patches);
        _silo.AddService(NullLogger<PatchManager>.Instance);
    }

    protected static FakePatch APatch(string name, SemanticVersion version) => new(name, version);

    protected async Task CreateManager()
    {
        _silo.AddService<IOptions<PatchingOptions>>(Options.Create(new PatchingOptions { HostVersion = _version }));
        _manager = await _silo.CreateGrainAsync<PatchManager>(0);
    }
}
