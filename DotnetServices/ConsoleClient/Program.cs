using SharedClasses;
using SharedClasses.ResponseDto;
using System.Net.Http.Json;

string prompt = """
Явления:
    Скорость машины от 0 до 90.
    Погода на улице (ясно, облачно, дождь).
    Светло ли на улице.

Начальное состояние:
    Машина едет со скоростью 78 км/ч в дождь ночью.
""";


UserRequestDto body = new()
{
    Prompt = prompt
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

    Console.WriteLine(responsePart?.SerializeMultiLine() ?? "Error: response part is null");
}
