// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.InMemory;

namespace Cratis.Choreography.Operations.for_OperationHistoryStorage.given;

public class an_operation_history_storage : Specification
{
    protected OperationHistoryStorage Storage;

    void Establish() => Storage = new();

    protected static OperationEntry Entry(string scope, string actor) =>
        new(OperationId.New(), 0, scope, actor, "kind", OperationRole.Forward, OperationStatus.Started, [], DateTimeOffset.UnixEpoch, null, null, null, null);
}
