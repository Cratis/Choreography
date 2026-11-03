// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_fanning_out.given;

public class a_fan_out : for_WorkflowCoordinator.given.a_coordinator
{
    protected void AdmitFanOut(int maxParallelism = 10, JoinMode mode = JoinMode.All) => Admit(
        [Trigger(), Action("map"), ForEach("each-target", "map", "targets", maxParallelism), Action("publish"), Join("gather", "each-target", mode)],
        [Edge("trigger", "next", "map"), Edge("map", "next", "each-target"), Edge("each-target", "each", "publish"), Edge("publish", "next", "gather")]);

    protected static string Targets(params string[] keys) =>
        "{\"targets\":[" + string.Join(',', keys.Select(key => $"{{\"key\":\"{key}\",\"language\":\"{key}\"}}")) + "]}";
}
