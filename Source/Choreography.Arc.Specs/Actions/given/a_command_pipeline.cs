// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Choreography.Actions;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Choreography.Arc.Actions.given;

public abstract class a_command_pipeline : Specification
{
    protected ICommandPipeline _pipeline;
    protected ActionContext _context;
    protected List<object> _executed = [];

    void Establish()
    {
        _pipeline = Substitute.For<ICommandPipeline>();
        var services = new ServiceCollection().AddSingleton(_pipeline).BuildServiceProvider();
        _context = new(new(GrainKind.StateMachine, "a-type", "a-key"), services);
        PipelineReturns(CommandResult.Success(CorrelationId.New()));
    }

    protected void PipelineReturns(CommandResult result) => _pipeline
        .Execute(Arg.Any<object>(), Arg.Any<IServiceProvider>(), Arg.Any<ValidationResultSeverity?>())
        .Returns(call =>
        {
            _executed.Add(call.Arg<object>());
            return Task.FromResult(result);
        });

    public class CommandForTesting
    {
        public string Name { get; set; } = string.Empty;
    }

    public record RecordCommandForTesting(string Name);
}
