// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Jobs.Integration.Recovery;

public interface IRecoveryJob : IJob<RecoveryRequest>
{
    Task<SiloAddress> Silo();
    Task<JobState> Inspect();
}

[GenerateSerializer]
public sealed record RecoveryRequest : IJobRequest;
