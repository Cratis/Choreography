---
title: Serializers and typed grain keys
description: The Orleans codecs Choreography registers for Cratis types, how the JSON fallback decides what it owns, and how KeyHelper builds compound grain keys.
---

Orleans generates codecs for types it knows at compile time. Cratis types cross grain boundaries without one: concepts
(`ConceptAs<T>`), OneOf values, expando objects and LINQ iterators. If the silo cannot encode them, a grain call that works on
one silo fails the moment it crosses to another. `AddCratisChoreography` registers the missing codecs for you; this page
explains what they own, so you can tell when a type of yours needs its own.

## The registered serializers

`AddCratisChoreographySerializers` on `IServiceCollection` registers all of these. Register it yourself only when you
[configure Orleans without the helper](hosting/configure-orleans-yourself.md).

| Serializer | Owns |
| --- | --- |
| `ConceptSerializer` | Every `ConceptAs<T>` that crosses a grain boundary, such as job ids and names. It writes the concept type, then the underlying primitive value. |
| `OneOfSerializer` | `OneOf<T0, T1, ...>` values and `OneOfBase` derivatives such as `Result<,>`. It writes the concrete type, the active case index and the active value. |
| `ExpandoObjectSerializer` | `ExpandoObject` payloads, copied through the dictionary copier. |
| `LinqCollectionCopier` | Deep copies of the internal types LINQ and collection expressions produce, which Orleans cannot copy. It materializes them to an array. |
| `CratisJsonSerializer` | Everything else that is Cratis-owned and has no generated codec, carried as System.Text.Json. |
| `CratisTypesFilter` | Allows the types in namespaces that start with `Cratis`, including arrays of them, so interface-typed payloads pass the Orleans type manifest on cross-silo calls. |

The serializers that also act as type filters (`ConceptSerializer`, `OneOfSerializer`, `LinqCollectionCopier`, `CratisJsonSerializer`
and `CratisTypesFilter`) allow
only the plain types and generic type definitions they own. A closed generic type, such as `Result<string, JobId>`, is left to
Orleans, which allows it only when each generic argument is allowed on its own. Which serializer writes the value does
not change.

The public registration helpers are `AddConceptSerializer()`, `AddLinqCollectionCopier()` and
`AddCompleteSerializer<TSerializer>()` for a serializer that implements `IGeneralizedCodec`, `IGeneralizedCopier` and
`ITypeFilter` together.

:::note[A complete serializer is instantiated once per interface]
`AddCompleteSerializer<TSerializer>()` registers one singleton descriptor per interface, so state kept in instance fields is
shared only among the members of one interface. Put state that must be shared in its own singleton and inject it.
:::

## What the JSON fallback owns

`CratisJsonSerializer` claims a type when it lives in a namespace that starts with `Cratis`, is in `OneOf.Types`, or is a
`JsonObject`. It never claims abstract or framework types, interfaces or `OneOfBase` derivatives; the OneOf serializer owns the
last. Because the test is the namespace, a type of yours in a `Cratis.*` namespace goes through JSON without any attribute, and a
type in any other namespace needs a generated Orleans codec (`[GenerateSerializer]`) or a serializer you register.

The fallback is a codec of its own, not a claim on Orleans' shared `JsonCodec`. The shared codec keeps one set of
`JsonSerializerOptions`, and every `AddJsonSerializer` call overwrites it. A host registering a JSON serializer for its own
namespaces, as hosts naturally do after the Cratis setup, would otherwise replace the options Cratis types depend on. Owning the
codec keeps host configuration from reaching them.

It serializes with camel-case property names, concepts as their underlying values through `ConceptAsJsonConverterFactory`, and the
polymorphic `JobState.Request` through `JobStateConverter`, which resolves the concrete request type from the job type registry.
The wire format matches Orleans' own JSON codec: the concrete type, then the JSON payload, so the runtime type round-trips
between silos. A deep copy inside one silo is the same serialize and deserialize pair.

When a OneOf type cannot be rebuilt on read, the serializer throws `CannotReconstructOneOf`. The type must be a `OneOf<...>` or
expose a constructor that takes the underlying `OneOf<...>` value.

## Typed grain keys

Orleans compound keys are strings. `KeyHelper` combines parts with `#` (`KeyHelper.Separator`) and parses them back into a
record, concepts included:

```csharp
using Cratis.Choreography;
using Cratis.Concepts;

public record OrderId(Guid Value) : ConceptAs<Guid>(Value);

public record OrderKey(OrderId Order, string Region, string? Channel)
{
    public override string ToString() => KeyHelper.Combine(Order, Region, Channel!);

    public static OrderKey Parse(string key) => KeyHelper.Parse<OrderKey>(key);
}
```

`Combine` joins the parts with `#` and drops `null` parts. `Parse<T>` splits on `#` and builds the record through the first
constructor of `T`, converting each element to its parameter type:

- A `string` parameter takes the element as is.
- A concept parameter is rebuilt through the concept factory.
- Any other type goes through `Convert.ChangeType`.
- An empty or missing element becomes `null`. That is how a default, empty namespace travels: `my-store#` parses back to a key
  with a `null` namespace.

The reflected constructor and parameters are memoized per type, so repeated parsing is a dictionary lookup.

:::caution[Keep parts free of the separator and trailing-only optional]
A part that contains `#` splits into two elements on parse. Because `Combine` drops `null` parts, only trailing parts may be
optional: a `null` in the middle shifts every later part one position to the left.
:::

The job system's `JobKey`, `JobStepKey` and `JobsManagerKey` are built on exactly this.

## Other helpers

| Helper | What it does |
| --- | --- |
| `GrainExtensions.GetGrainType(IGrain)` in `Orleans` | Returns the grain interface type, either from `IGrainType` or by the `IFoo` to `Foo` naming convention. Throws `InvalidGrainName` when no interface matches. |
| `GrainExtensions.GetReference<T>(IGrainFactory, IGrainWithGuidCompoundKey)` in `Orleans` | Resolves a compound-key grain you hold as an instance back to a reference of interface `T`. |
| `ObjectExtensions.DeserializeIfNecessary<TResult>` in `Cratis.Choreography.Json` | Returns an object as `TResult`, deserializing from a `JsonElement` or by a JSON round trip when it is not already one. |

## Next steps

- [Tenancy across grain calls](tenancy.md) shows how job keys carry the tenant.
- [Troubleshooting](reference/troubleshooting.md) covers serialization failures that appear only across silos.
