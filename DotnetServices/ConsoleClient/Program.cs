using SharedClasses;
using SharedClasses.ResponseDto;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

string userPrompt = """
Явления:
    Яркость лампы (целое число от 0 до 100).
    Режим освещения (день, ночь, вечер).
    Датчик движения (включен или выключен).

Начальное состояние:
    Яркость лампы 0.
    Режим освещения вечер.
    Датчик движения выключен.

Условие:
    Датчик движения включен И (режим освещения ночь ИЛИ яркость меньше 30).
""";



UserRequestDto body = new()
{
    Prompt = userPrompt
};


using HttpRequestMessage request = new()
{
    Method = HttpMethod.Post,
    RequestUri = new Uri("http://localhost:5155/api/chat"),
    Content = JsonContent.Create(body)
};

using HttpClient client = new();

using HttpResponseMessage response = await client.SendAsync(
    request,
    HttpCompletionOption.ResponseHeadersRead
);

using Stream stream = await response.Content.ReadAsStreamAsync();

using StreamReader reader = new(stream);

while (true)
{
    string? line = await reader.ReadLineAsync();
    if (line == null) break;


    ResponseDtoBase? responsePart = ResponseDtoBase.FromString(line);

    if(responsePart is ChunkDto chunk)
    {
        string text = Regex.Unescape(chunk.ChunkText);

        Console.Write(text);
    }
    else
        Console.WriteLine(responsePart?.SerializeMultiLine() ?? "Error: response part is null");
}
