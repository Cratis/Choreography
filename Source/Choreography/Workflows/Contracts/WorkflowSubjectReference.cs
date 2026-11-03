// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Contracts;

/// <summary>
/// What a workflow run is about.
/// </summary>
/// <param name="Kind">The <see cref="WorkflowSubjectKind"/>.</param>
/// <param name="Id">The <see cref="WorkflowSubjectId"/>.</param>
public record WorkflowSubjectReference(WorkflowSubjectKind Kind, WorkflowSubjectId Id);
