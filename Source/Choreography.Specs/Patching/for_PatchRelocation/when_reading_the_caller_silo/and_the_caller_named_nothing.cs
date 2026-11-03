// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchRelocation.when_reading_the_caller_silo;

public class and_the_caller_named_nothing : Specification
{
    SiloAddress? _result;
    void Establish() => RequestContext.Remove(PatchRelocation.CallerSiloKey);
    void Because() => _result = PatchRelocation.CallerSilo();
    [Fact] void should_not_name_a_silo() => _result.ShouldBeNull();
}
