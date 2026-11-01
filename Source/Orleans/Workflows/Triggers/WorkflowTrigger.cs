// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Contracts;

namespace Cratis.Orleans.Workflows.Triggers;

/// <summary>
/// What asks a workflow to run.
/// </summary>
/// <param name="Kind">The <see cref="WorkflowTriggerKind"/>.</param>
/// <param name="Source">The <see cref="TriggerSourceKey"/> naming where it came from.</param>
/// <param name="Deduplication">The <see cref="WorkflowDeduplicationKey"/> - the same key is the same run.</param>
/// <param name="Subject">The <see cref="WorkflowSubjectReference"/> the run is about.</param>
/// <param name="Contract">The <see cref="WorkflowContractReference"/> the payload follows.</param>
/// <param name="Payload">The payload, as <see cref="WorkflowData"/>.</param>
/// <param name="Evidence">The evidence the trigger carries.</param>
/// <param name="Correlation">The <see cref="WorkflowCorrelationKey"/> the outside world refers to the run by.</param>
/// <param name="Causation">The <see cref="WorkflowCausationKey"/> of whatever caused the trigger.</param>
/// <param name="Actor">The <see cref="WorkflowActor"/> behind it.</param>
/// <param name="OccurredAt">When it happened.</param>
/// <remarks>
/// A trigger never names a scope: the run belongs to the scope of the grain that admits it.
/// </remarks>
public record WorkflowTrigger(
    WorkflowTriggerKind Kind,
    TriggerSourceKey Source,
    WorkflowDeduplicationKey Deduplication,
    WorkflowSubjectReference Subject,
    WorkflowContractReference Contract,
    WorkflowData Payload,
    IEnumerable<EvidenceReference> Evidence,
    WorkflowCorrelationKey Correlation,
    WorkflowCausationKey Causation,
    WorkflowActor Actor,
    DateTimeOffset OccurredAt);
