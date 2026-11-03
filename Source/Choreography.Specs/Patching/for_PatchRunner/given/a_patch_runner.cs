// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Patching.for_PatchRunner.given;

public class a_patch_runner : Specification
{
    protected IGrainFactory _grainFactory;
    protected IPatchManager _manager;
    protected ILocalSiloDetails _siloDetails;
    protected RecordingTimeProvider _timeProvider;
    protected PatchingOptions _options;
    protected SemanticVersion _version = new(19, 100, 0);
    protected PatchRunner _runner;
    protected CancellationTokenSource _cancellation = new();

    void Establish()
    {
        _manager = Substitute.For<IPatchManager>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _grainFactory.GetGrain<IPatchManager>(0, null).Returns(_manager);
        _siloDetails = Substitute.For<ILocalSiloDetails>();
        _timeProvider = new();
        _options = new PatchingOptions { HostVersion = _version };
        _runner = new PatchRunner(_grainFactory, _siloDetails, Options.Create(_options), _timeProvider, NullLogger<PatchRunner>.Instance);
    }

    protected static OrleansException AwaitingManifest() => new(PatchRunner.PlacementFailurePrefix + "0. Known nodes with grain type: none.");

    protected Task Apply() => _runner.ApplyPatches(_cancellation.Token);
}
