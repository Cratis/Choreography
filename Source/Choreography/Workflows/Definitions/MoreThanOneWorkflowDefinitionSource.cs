// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Definitions;

/// <summary>
/// The exception that is thrown when more than one source of workflow definitions claims the same workflow.
/// </summary>
/// <param name="workflow">The workflow claimed twice.</param>
public class MoreThanOneWorkflowDefinitionSource(WorkflowKey workflow)
    : Exception($"More than one source of workflow definitions claims the workflow '{workflow.Workflow.Value}'");
