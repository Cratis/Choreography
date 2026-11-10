// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Tools.GrainCompatibility.for_ContractManifest.when_parsing;

public class and_a_manifest_has_duplicate_or_malformed_entries : Specification
{
    const string Interface = """interface [GrainInterfaceType("IOne")] IOne [Version(1)]""";

    static Exception Parse(string text) => Catch.Exception(() => ContractManifest.Parse(text))!;

    [Fact] void should_refuse_a_duplicate_interface() => Parse($"{Interface}\n{Interface}").ShouldBeOfExactType<InvalidContractManifest>();
    [Fact] void should_refuse_a_duplicate_grain_class() => Parse("class [GrainType(\"a\")] A\nclass [GrainType(\"a\")] A").ShouldBeOfExactType<InvalidContractManifest>();
    [Fact] void should_refuse_a_duplicate_method_identity() => Parse($"{Interface}\n  AA: One() -> Task\n  AA: Two() -> Task").ShouldBeOfExactType<InvalidContractManifest>();
    [Fact] void should_refuse_a_method_without_a_wire_identity() => Parse($"{Interface}\n  One() -> Task").ShouldBeOfExactType<InvalidContractManifest>();
    [Fact] void should_refuse_a_method_without_a_parameter_list() => Parse($"{Interface}\n  AA: One").ShouldBeOfExactType<InvalidContractManifest>();
    [Fact] void should_refuse_a_method_after_a_grain_class() => Parse($"{Interface}\nclass [GrainType(\"a\")] A\n  AA: One() -> Task").ShouldBeOfExactType<InvalidContractManifest>();
    [Fact] void should_report_the_line_number() => Parse($"{Interface}\n{Interface}").Message.StartsWith("Line 2:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_accept_windows_line_endings_and_comments() => ContractManifest.Parse($"# comment\r\n{Interface}\r\n  AA: One() -> Task\r\n").Interfaces["IOne"].Methods.Count.ShouldEqual(1);
}
