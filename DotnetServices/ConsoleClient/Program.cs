using SharedClasses;
using SharedClasses.ResponseDto;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

string userPrompt = """
Сделай модель для умного климат-контроля. Режимы работы системы: Выключен, Обогрев, Охлаждение. Целевая температура задается в строгом диапазоне от 16 до 30 градусов. Также есть датчик открытого окна (включен или выключен).
По умолчанию система выключена, целевая температура установлена на 20, окно закрыто.
Важное правило безопасности: если окно открыто, система физически не может находиться в режимах Обогрева или Охлаждения. Мы хотим быть уверены, что при нормальных условиях можно перевести систему в режим Обогрева. Также проверь, нет ли в системе тупиковых состояний (например, если окно открыто, у нас всё равно должна быть возможность выключить систему или закрыть окно, чтобы не зависнуть навсегда).
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
