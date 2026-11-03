// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.InMemory;

namespace Cratis.Choreography.Operations.for_OperationHistory.given;

public class an_operation_history : Specification
{
    protected OperationHistory History;

    void Establish() => History = new(new OperationHistoryStorage(), TimeProvider.System);
}
