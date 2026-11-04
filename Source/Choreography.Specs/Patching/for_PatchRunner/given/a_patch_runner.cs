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

    /// <summary>
    /// Orleans keeps the constructor of its message rejection exception internal, so a spec that needs one creates it the way Orleans does.
    /// </summary>
    /// <param name="message">The message of the exception.</param>
    /// <returns>An <see cref="OrleansMessageRejectionException"/>.</returns>
    protected static OrleansMessageRejectionException Rejection(string message) =>
        (OrleansMessageRejectionException)Activator.CreateInstance(typeof(OrleansMessageRejectionException), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, [message], null)!;

    protected Task Apply() => _runner.ApplyPatches(_cancellation.Token);
}
