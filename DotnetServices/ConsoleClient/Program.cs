using SharedClasses;
using System.Net.Http.Json;

string prompt = """
Требования:
    Необходимо сформировать переменные состояния.
    Доступны 3 типа: Integer, Enum, Boolean.
    ВАЖНО: Поле 'EnumValues' должно заполняться ТОЛЬКО если VarType == 'Enum'.
    Для типов Integer и Boolean поле 'EnumValues' должно БЫТЬ СТРОГО null (не пустой список []).
    Также необходимо заполнить текущие значения переменных.
    Укажи что ты за llm в поле LlmName.


Явления:
    Скорость машины от 0 до 90.
    Погода на улице (ясно, облачно, дождь).
    Светло ли на улице.


Начальное состояние:
    Машина едет со скоростью 78 км/ч в дождь ночью. 
""";


Console.WriteLine(prompt);



UserRequestDto body = new()
{
    Prompt = prompt
};


using HttpRequestMessage request = new()
{
    Method = HttpMethod.Post,
    RequestUri = new Uri("http://localhost:5155/api/chat/test"),
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
    Console.WriteLine(line);
}

