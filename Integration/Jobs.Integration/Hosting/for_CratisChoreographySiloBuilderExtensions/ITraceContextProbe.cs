// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Jobs.Integration.Hosting.for_CratisChoreographySiloBuilderExtensions;

public interface ITraceContextProbe : IGrainWithGuidKey
{
    Task<string[]> Read(bool relay);
}
