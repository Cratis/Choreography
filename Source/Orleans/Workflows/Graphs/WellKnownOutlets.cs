// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Graphs;

/// <summary>
/// The outlets the engine itself gives meaning to.
/// </summary>
public static class WellKnownOutlets
{
    /// <summary>The ordinary way on.</summary>
    public static readonly OutletName Next = "next";

    /// <summary>Taken when a node failed for good.</summary>
    public static readonly OutletName Failed = "failed";

    /// <summary>Taken when a node answered with something it did not declare.</summary>
    public static readonly OutletName Fallback = "fallback";

    /// <summary>Taken once per item by a for-each.</summary>
    public static readonly OutletName Each = "each";

    /// <summary>Taken by a join whose mode was satisfied.</summary>
    public static readonly OutletName Completed = "completed";

    /// <summary>Taken by a join whose mode was not satisfied when every branch had finished.</summary>
    public static readonly OutletName Partial = "partial";

    /// <summary>Taken by a signal wait that was resumed.</summary>
    public static readonly OutletName Resumed = "resumed";

    /// <summary>Taken by a timer wait once its time has come.</summary>
    public static readonly OutletName Elapsed = "elapsed";

    /// <summary>Taken by a signal or approval wait that ran out of time.</summary>
    public static readonly OutletName Expired = "expired";

    /// <summary>Taken by an approval that was given.</summary>
    public static readonly OutletName Approved = "approved";

    /// <summary>Taken by an approval that was refused.</summary>
    public static readonly OutletName Rejected = "rejected";

    /// <summary>Taken by a signal wait that was called off.</summary>
    public static readonly OutletName Cancelled = "cancelled";
}
