// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using Orleans.Serialization;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Serializers;
using Orleans.Serialization.WireProtocol;

namespace Cratis.Choreography.Serialization;

/// <summary>
/// Represents a custom Orleans serializer for <see cref="ExpandoObject"/>.
/// </summary>
/// <param name="codecProvider">The <see cref="ICodecProvider"/>.</param>
public class ExpandoObjectSerializer(ICodecProvider codecProvider) : IGeneralizedCodec, IGeneralizedCopier, ITypeFilter
{
    /// <inheritdoc/>
    public object? DeepCopy(object? input, CopyContext context)
    {
        if (input is not ExpandoObject original)
        {
            return input;
        }

        var dictionary = (IDictionary<string, object?>)original;
        var dictionaryCopier = codecProvider.GetDeepCopier<Dictionary<string, object?>>();
        var copiedDictionary = dictionaryCopier.DeepCopy(new Dictionary<string, object?>(dictionary), context);

        var result = new ExpandoObject();
        var resultDict = (IDictionary<string, object?>)result;
        foreach (var kvp in copiedDictionary)
        {
            resultDict[kvp.Key] = kvp.Value;
        }

        return result;
    }

    /// <inheritdoc/>
    public bool IsSupportedType(Type type) => type == typeof(ExpandoObject);

    /// <inheritdoc/>
    public bool? IsTypeAllowed(Type type)
    {
        if (type == typeof(ExpandoObject))
        {
            return true;
        }
        return null;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The object is written through the dictionary codec, which marks it and every <see cref="ExpandoObject"/>
    /// nested in it as a <see cref="Dictionary{TKey, TValue}"/>. Read back as it was written, a child in a
    /// collection or a nested object arrived on the receiving silo as a dictionary, and code that looks a child
    /// up by its properties could no longer find them (Cratis/Chronicle#4337). The nested dictionaries the
    /// dictionary codec produces are therefore turned back into <see cref="ExpandoObject"/>s here. What goes on
    /// the wire is unchanged, so silos on either side of this change keep understanding each other during a
    /// rolling update.
    /// </remarks>
    public object ReadValue<TInput>(ref Reader<TInput> reader, Field field)
    {
        var dictionaryCodec = codecProvider.GetCodec<Dictionary<string, object?>>();
        return ToExpandoObject(dictionaryCodec.ReadValue(ref reader, field)!);
    }

    /// <inheritdoc/>
    public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, [AllowNull] Type expectedType, [AllowNull] object? value)
        where TBufferWriter : IBufferWriter<byte>
    {
        if (value is not ExpandoObject expandoObject)
        {
            return;
        }

        var dictionary = new Dictionary<string, object?>(expandoObject);
        var dictionaryCodec = codecProvider.GetCodec<Dictionary<string, object?>>();
        dictionaryCodec.WriteField(ref writer, fieldIdDelta, expectedType, dictionary);
    }

    static ExpandoObject ToExpandoObject(Dictionary<string, object?> dictionary)
    {
        var result = new ExpandoObject();
        var resultAsDictionary = (IDictionary<string, object?>)result;
        foreach (var (key, value) in dictionary)
        {
            resultAsDictionary[key] = Restore(value);
        }

        return result;
    }

    static object? Restore(object? value)
    {
        switch (value)
        {
            case Dictionary<string, object?> nested:
                return ToExpandoObject(nested);

            case IList<object?> items:
                for (var index = 0; index < items.Count; index++)
                {
                    items[index] = Restore(items[index]);
                }

                return items;

            default:
                return value;
        }
    }
}
