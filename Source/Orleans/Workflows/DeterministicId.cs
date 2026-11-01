// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Orleans.Workflows;

/// <summary>
/// Derives a stable <see cref="Guid"/> from a tuple of strings - the identity of something whose
/// natural key is composite, such as a discovered resource (connection + provider-native id + type).
/// </summary>
/// <remarks>
/// A name-based UUID in the style of RFC 9562 version 5: SHA-1 over a fixed namespace followed by
/// every part, each prefixed with its UTF-8 byte length, with the version and variant bits set.
/// <para>
/// The length prefix is what makes this a tuple rather than a concatenation. Joining parts with a
/// separator - the dash-joined strings this replaces - lets <c lang="csharp">("a-b", "c")</c> and <c lang="csharp">("a", "b-c")</c>
/// collide, and a separator that can occur inside a part always eventually does. With a length prefix
/// no two different tuples produce the same bytes.
/// </para>
/// <para>
/// Deterministic identities are what make discovery idempotent: the same resource discovered again
/// on the next pass, by the next process, after a restart, is the same event source.
/// </para>
/// </remarks>
internal static class DeterministicId
{
    /// <summary>
    /// The namespace every identity is derived under - fixed forever, because changing it changes
    /// every identity ever derived.
    /// </summary>
    public static readonly Guid Namespace = new("6f1d4a5e-2c3b-4e8a-9d71-0b5c8e3f2a14");

    /// <summary>
    /// Derives the identity of a tuple.
    /// </summary>
    /// <param name="parts">The parts of the natural key, in a fixed order.</param>
    /// <returns>The same <see cref="Guid"/> for the same parts, every time.</returns>
    public static Guid From(params string[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);

        using var buffer = new MemoryStream();
        buffer.Write(Namespace.ToByteArray(bigEndian: true));

        Span<byte> length = stackalloc byte[4];
        foreach (var part in parts)
        {
            var bytes = Encoding.UTF8.GetBytes(part ?? string.Empty);
            BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            buffer.Write(length);
            buffer.Write(bytes);
        }

#pragma warning disable CA5350 // SHA-1 is what name-based version 5 UUIDs are defined over; this is identity, not security.
        var hash = SHA1.HashData(buffer.ToArray());
#pragma warning restore CA5350

        var identity = hash[..16];
        identity[6] = (byte)((identity[6] & 0x0F) | 0x50);
        identity[8] = (byte)((identity[8] & 0x3F) | 0x80);
        return new Guid(identity, bigEndian: true);
    }
}
