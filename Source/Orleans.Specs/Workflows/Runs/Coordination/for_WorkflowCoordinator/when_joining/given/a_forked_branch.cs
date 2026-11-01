// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_joining.given;

public class a_forked_branch : for_WorkflowCoordinator.given.a_coordinator
{
    void Establish()
    {
        Admit([Trigger(), Action("map"), ForEach("each", "map", "items"), Action("fast"), Action("slow"), Join("join", "each")],
            [Edge("trigger", "next", "map"), Edge("map", "next", "each"), Edge("each", "each", "fast"),
             Edge("each", "each", "slow"), Edge("fast", "next", "join"), Edge("slow", "next", "join")]);
        Succeed(Root("map"), output: "{\"items\":[{\"key\":\"one\"}]}");
        Succeed(InBranch("fast", "each", "one"));
    }
}
