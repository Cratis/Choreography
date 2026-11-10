// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Choreography.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    /// <summary>
    /// Refuses a start that the job's status rules out.
    /// </summary>
    /// <returns>The refusal, or null when the status allows the start.</returns>
    Result<StartJobError>? StartRefusedByState()
    {
        if (State.Status is JobStatus.Failed)
        {
            return StartJobError.Unknown;
        }
        if (State.Status is JobStatus.CompletedSuccessfully or JobStatus.CompletedWithFailures)
        {
            return StartJobError.AlreadyCompleted;
        }
        if (JobIsPrepared() || State.Status is JobStatus.PreparingJob or JobStatus.PreparingSteps)
        {
            return StartJobError.AlreadyBeenPrepared;
        }

        return null;
    }

    /// <summary>
    /// Asks whether the job may start.
    /// </summary>
    /// <param name="request">The request the job starts with.</param>
    /// <returns>The refusal or failure, or null when the job is admitted.</returns>
    async Task<Result<StartJobError>?> AnswerStartFromAdmission(TRequest request)
    {
        switch (await Admit(request, "Start"))
        {
            case Admission.Refused:
                if (ExecutionGeneration() == 0)
                {
                    DeactivateOnIdleDuringShutdown();
                }

                return StartJobError.NotAdmitted;
            case Admission.Failed:
                return StartJobError.Unknown;
        }

        return null;
    }
}
