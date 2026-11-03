// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Jobs.Integration.Recovery;

public interface IRecoveryStep : IJobStep<RecoveryRequest, string, JobStepState>;
