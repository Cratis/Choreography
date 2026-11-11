// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Tools.GrainCompatibility.for_ContractManifest.when_parsing;

public class and_a_retired_interface_has_a_method : Specification
{
    ContractManifest _manifest = null!;

    void Because() => _manifest = ContractManifest.Parse("""
        *RETIRED* interface [GrainInterfaceType("IOne")] IOne [Version(1)]
          AA: One() -> Task
        """);

    [Fact] void should_keep_the_interface_retired() => _manifest.Interfaces["IOne"].Retired.ShouldBeTrue();
    [Fact] void should_attach_the_method_to_the_retired_interface() => _manifest.Interfaces["IOne"].Methods.Keys.ShouldContainOnly("AA");
}
