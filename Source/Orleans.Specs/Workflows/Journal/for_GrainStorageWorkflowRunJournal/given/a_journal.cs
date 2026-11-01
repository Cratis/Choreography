// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Journal.for_GrainStorageWorkflowRunJournal.given;

public class a_journal : Specification
{
    protected a_storage Storage = null!;
    protected GrainStorageWorkflowRunJournal Journal = null!;
    protected WorkflowRunKey Key = new(new WorkflowScope("scope"), new WorkflowRunId("run-1"));

    void Establish()
    {
        Storage = new();
        Journal = Storage.Journal();
    }
}
