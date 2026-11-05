// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Choreography.Collaboration.for_CollaborationRetry;

public class when_the_call_never_completes : Specification
{
    readonly TaskCompletionSource<bool> _pending = new();
    Result<bool, CollaborationUnavailable> _result;

    async Task Because() => _result = await new CollaborationRetry(_ => Task.CompletedTask, TimeSpan.FromMilliseconds(10))
        .Execute(() => _pending.Task, () => { });

    [Fact] void should_return_unavailable_at_the_deadline() => _result.TryGetResult(out _).ShouldBeFalse();
    [Fact] void should_not_wait_for_the_orleans_response_timeout() => _pending.Task.IsCompleted.ShouldBeFalse();
}
