// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Triggers;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Choreography.Chronicle.Workflows;

/// <summary>
/// A declaration that an event admits a run of a workflow.
/// </summary>
/// <param name="Reactor">The <see cref="ReactorId"/> of the reactor that delivers the event.</param>
/// <param name="Workflow">The <see cref="WorkflowId"/> of the workflow to admit a run of.</param>
/// <param name="EventType">The type of event.</param>
/// <param name="Define">Declares the reactor's handler, given the function that admits the trigger the handler built.</param>
public record WorkflowEventTrigger(
    ReactorId Reactor,
    WorkflowId Workflow,
    Type EventType,
    Action<IReactorBuilder, Func<WorkflowTrigger?, EventContext, Task>> Define);
