// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Tools.GrainCompatibility.for_ContractManifest.when_parsing;

public class and_an_interface_type_contains_an_escaped_quote : Specification
{
    ContractManifest _manifest = null!;

    void Because() => _manifest = ContractManifest.Parse("""interface [GrainInterfaceType("a\"b")] IOne [Version(1)]""");

    [Fact] void should_unescape_the_interface_key() => _manifest.Interfaces.Keys.ShouldContainOnly("a\"b");
}
