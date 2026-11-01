// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Chronicle.Workflows;

/// <summary>
/// The JSON every fact in <see cref="WorkflowFactSamples"/> is written as. Stores already hold facts in exactly this shape,
/// so these literals are a contract: when one of them has to change, the change is a break for every store that has the fact.
/// </summary>
public static class WorkflowFactJson
{
    public static readonly IReadOnlyDictionary<string, string> Expected = new Dictionary<string, string>
    {
        ["WorkflowRunAdmitted"] = """{"workflow":"workflow-1","area":"area-1","version":3,"catalogVersion":"catalog-7","capabilities":[{"capability":{"id":"capability-1","version":2},"provider":"provider-9"}],"graph":{"nodes":[{"id":"trigger","kind":0,"capability":{"id":"trigger","version":1},"configuration":"{\u0022a\u0022:1}","outlets":["next"],"policy":{"maxAttempts":3,"attemptTimeout":"00:02:00","retryDelay":"00:00:05","maxVisits":4}}],"edges":[{"from":"trigger","outlet":"next","to":"publish"}]},"trigger":{"kind":1,"source":"source-1","deduplication":"dedup-1","subject":{"kind":"subject-kind","id":"subject-1"},"contract":{"id":"contract-1","version":2},"payload":"{\u0022payload\u0022:true}","evidence":["evidence-1"],"correlation":"correlation-1","causation":"causation-1","actor":"someone","occurredAt":"2026-03-01T09:30:00\u002B00:00"}}""",
        ["WorkflowNodeScheduled"] = """{"execution":{"node":"publish","branch":"each//2:one","visit":2},"attempt":2,"job":"job-1","wave":5}""",
        ["WorkflowNodeOperationCompleted"] = """{"execution":{"node":"publish","branch":"each//2:one","visit":2},"attempt":2,"operation":"register","output":"{\u0022done\u0022:1}"}""",
        ["WorkflowNodeSucceeded"] = """{"execution":{"node":"publish","branch":"each//2:one","visit":2},"attempt":2,"outlet":"next","output":"{\u0022out\u0022:1}","evidence":["evidence-2"]}""",
        ["WorkflowNodeSkipped"] = """{"execution":{"node":"publish","branch":"each//2:one","visit":2},"reason":"nothing to do"}""",
        ["WorkflowNodeAttemptFailed"] = """{"execution":{"node":"publish","branch":"each//2:one","visit":2},"attempt":2,"kind":1,"reason":"too slow"}""",
        ["WorkflowNodeFailed"] = """{"execution":{"node":"publish","branch":"each//2:one","visit":2},"attempt":2,"kind":0,"reason":"it went wrong","routedTo":"failed"}""",
        ["WorkflowNodeOutcomeAmbiguous"] = """{"execution":{"node":"publish","branch":"each//2:one","visit":2},"attempt":2,"reason":"unknown outcome"}""",
        ["WorkflowNodesUnreachable"] = """{"executions":[{"node":"publish","branch":"each//2:one","visit":2}]}""",
        ["WorkflowBranchesFannedOut"] = """{"forEach":{"node":"publish","branch":"each//2:one","visit":2},"branches":[{"key":"branch-1","item":"{\u0022item\u0022:1}"}]}""",
        ["WorkflowBranchesJoined"] = """{"join":{"node":"publish","branch":"each//2:one","visit":2},"outlet":"completed","results":"{\u0022results\u0022:[]}"}""",
        ["WorkflowWaitOpened"] = """{"wait":"wait-1","execution":{"node":"publish","branch":"each//2:one","visit":2},"kind":2,"correlation":"correlation-1","signal":{"id":"contract-1","version":2},"deadline":"2026-03-01T09:30:00\u002B00:00"}""",
        ["WorkflowWaitResolved"] = """{"wait":"wait-1","outlet":"approved","payload":"{\u0022by\u0022:\u0022someone\u0022}","actor":"someone"}""",
        ["WorkflowWaitCancelled"] = """{"wait":"wait-1"}""",
        ["WorkflowRunSucceeded"] = "{}",
        ["WorkflowRunFailed"] = """{"reason":"it failed"}""",
        ["WorkflowRunCancelled"] = """{"reason":"called off"}""",
        ["WorkflowRunNeedsReconciliation"] = "{}",
        ["WorkflowNodeInputPinned"] = """{"execution":{"node":"publish","branch":"each//2:one","visit":2},"input":"{\u0022input\u0022:1}"}""",
        ["WorkflowJobStopped"] = """{"job":"job-1"}"""
    };
}
