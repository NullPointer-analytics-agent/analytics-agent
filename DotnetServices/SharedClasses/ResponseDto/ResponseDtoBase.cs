using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace SharedClasses.ResponseDto;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(BehaviourModelDto), "behaviour_model")]
[JsonDerivedType(typeof(ChunkDto), "chunk_dto")]
[JsonDerivedType(typeof(SystemMessageDto), "system_message_dto")]
public abstract class ResponseDtoBase
{
    protected static readonly JsonSerializerOptions MultiLine = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.Cyrillic, UnicodeRanges.BasicLatin),
    };

    protected static readonly JsonSerializerOptions OneLine = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.Cyrillic, UnicodeRanges.BasicLatin),
    };


    public string SerializeOneLine()
    {
        return JsonSerializer.Serialize(this, OneLine);
    }

    public string SerizlizeMultiLine()
    {
        return JsonSerializer.Serialize(this, MultiLine);
    }


    public static ResponseDtoBase? FromString(string input)
    {
        return JsonSerializer.Deserialize<ResponseDtoBase>(input, OneLine);
    }

}