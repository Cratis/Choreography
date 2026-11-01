// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Journal;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Orleans.Hosting.for_WorkflowsSiloBuilderExtensions;

public class when_a_host_registers_its_own_journal_first : Specification
{
    IWorkflowRunJournal _own = null!;
    ServiceProvider _provider = null!;

    void Because()
    {
        _own = Substitute.For<IWorkflowRunJournal>();
        _provider = new ServiceCollection().AddLogging().AddSingleton(Substitute.For<IGrainFactory>()).AddSingleton(_own).AddCratisOrleansWorkflows().BuildServiceProvider();
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_keep_the_hosts_journal() => _provider.GetRequiredService<IWorkflowRunJournal>().ShouldEqual(_own);
}
