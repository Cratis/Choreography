// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Orleans.Workflows.Sweeping.for_WorkflowScopes;

public class when_gathering_scopes : Specification
{
    IReadOnlyCollection<WorkflowScope> _all = null!;

    async Task Because()
    {
        var options = new WorkflowsOptions();
        options.Scopes.Add("configured");
        var source = Substitute.For<IWorkflowScopeSource>();
        source.GetScopes(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IEnumerable<WorkflowScope>>([new WorkflowScope("from-source"), new WorkflowScope("configured")]));
        var scopes = new WorkflowScopes(Options.Create(options), [source]);
        scopes.Remember(new WorkflowScope("seen"));
        scopes.Remember(new WorkflowScope("seen"));
        _all = await scopes.GetAll(CancellationToken.None);
    }

    [Fact] void should_include_the_configured_the_sourced_and_the_seen_once_each() => _all.Select(scope => scope.Value).Order().ShouldEqual(["configured", "from-source", "seen"]);
}
