using SharedClasses;
using SharedClasses.ResponseDto;
using System.Net.Http.Json;

string userPrompt = """
Явления:
    Диаметр мяча от 3 до 5.
    Находится ли мяч в воздухе.
    Цвет пола в помещении (красный, белый, зеленый)

Начальное состояние:
    Мяч диаметром 4 лежит на полу белого цвета.

Условие:
    Мяч на полу, ИЛИ (его диаметр равен 5 И цвет пола не красный)
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

    Console.WriteLine(responsePart?.SerializeMultiLine() ?? "Error: response part is null");
}
