using System.Text.Json.Serialization;

namespace SharedClasses;


[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TypeOfVariable
{
    Integer, Enum, Boolean
}
