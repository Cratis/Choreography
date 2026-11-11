// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Tools.GrainCompatibility.for_ContractManifest.when_parsing;

public class and_an_error_names_its_context : Specification
{
    const string Interface = """interface [GrainInterfaceType("IOne")] IOne [Version(1)]""";

    Exception _duplicate = null!;
    Exception _afterClass = null!;

    void Because()
    {
        _duplicate = Catch.Exception(() => ContractManifest.Parse($"{Interface}\n  AA: One() -> Task\n  AA: Two() -> Task"))!;
        _afterClass = Catch.Exception(() => ContractManifest.Parse($"{Interface}\nclass [GrainType(\"a\")] A\n  AA: One() -> Task"))!;
    }

    [Fact] void should_name_the_interface_with_a_duplicate_method() => _duplicate.Message.ShouldEqual("Line 3: Method identity 'AA' appears twice on IOne.");
    [Fact] void should_clear_the_interface_after_a_class() => _afterClass.Message.ShouldEqual("Line 3: A method appears before any interface.");
}
