// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Contracts;
using Cratis.Orleans.Workflows.Graphs;
using Cratis.Orleans.Workflows.Waits;

namespace Cratis.Orleans.Workflows.Runs;

/// <summary>
/// A wait was resolved - a signal arrived, a person answered, or it ran out of time and left through its
/// <c lang="csharp">elapsed</c> or <c lang="csharp">expired</c> outlet.
/// </summary>
/// <param name="Wait">The <see cref="WorkflowWaitId"/>.</param>
/// <param name="Outlet">The <see cref="OutletName"/> the waiting node leaves through.</param>
/// <param name="Payload">What came with the resolution, as <see cref="WorkflowData"/>.</param>
/// <param name="Actor">The <see cref="WorkflowActor"/> who resolved it.</param>
public record WorkflowWaitResolved(WorkflowWaitId Wait, OutletName Outlet, WorkflowData Payload, WorkflowActor Actor);
