// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationEngine;

public class when_defining_the_same_kind_twice : Specification
{
    Exception _exception;

    void Because()
    {
        var definitions = new OperationDefinitions();
        definitions.Define<given.RenameModule>();
        _exception = Catch.Exception(() => definitions.Define<given.RenameModule>());
    }

    [Fact] void should_refuse() => _exception.ShouldBeOfExactType<OperationAlreadyDefined>();
}
