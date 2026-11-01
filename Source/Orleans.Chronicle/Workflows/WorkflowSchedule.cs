// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows;

namespace Cratis.Orleans.Chronicle.Workflows;

/// <summary>
/// A declaration that a workflow starts on a schedule.
/// </summary>
/// <param name="Name">The name of the schedule - part of the deduplication key of every run it starts.</param>
/// <param name="Scope">The <see cref="WorkflowScope"/> the workflow belongs to.</param>
/// <param name="Workflow">The <see cref="WorkflowId"/> of the workflow.</param>
/// <param name="Cron">The <see cref="CronExpression"/> saying when it starts.</param>
public record WorkflowSchedule(string Name, WorkflowScope Scope, WorkflowId Workflow, CronExpression Cron);
