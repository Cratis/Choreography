// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Availability;

/// <summary>
/// Evaluates availability gates. Forwards to <see cref="Cratis.Arc.Availability.AvailabilityEvaluator"/>.
/// </summary>
/// <param name="catalog">The catalog of definitions the evaluator reads.</param>
[Obsolete("The availability engine moved to Cratis.Arc.Availability (the Cratis.Arc.Availability package); use AvailabilityEvaluator there. This forwarder is removed in the next major release.")]
public class AvailabilityEvaluator(Cratis.Arc.Availability.AvailabilityCatalog catalog) : Cratis.Arc.Availability.AvailabilityEvaluator(catalog);
