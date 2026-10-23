using SharedClasses;
using SharedClasses.ResponseDto;
using System.Net.Http.Json;

string prompt = """
System: Твоя задача — проанализировать запрос пользователя и сформировать переменные состояния.
Доступны 3 типа: Integer, Enum, Boolean.
Поле 'EnumValues' заполняется ТОЛЬКО если VarType == 'Enum'. Для Integer и Boolean поле 'EnumValues' должно быть null.
В поле LlmName укажи свое имя.

КРИТЕРИИ ВАЛИДНОСТИ:
Запрос ВАЛИДЕН, если пользователь описывает список состояний (явлений) И их начальную конфигурацию. 
Запрос НЕВАЛИДЕН, если он не по теме (например, просьба посчитать, рассказать фильм) или в нем не хватает данных.

ПРАВИЛА ФОРМИРОВАНИЯ И ВЫВОДА JSON:
Выводи ответ СТРОГО в формате JSON. Структура зависит от валидности запроса:

ЕСЛИ ЗАПРОС ВАЛИДЕН:
{
  "LlmName": "Имя_Модели",
  "IsValidPrompt": true,
  "ReasonIfInvalid": "",
  "Definitions": [ /* список определений переменных */ ],
  "Values": [ /* список текущих значений */ ]
}

ЕСЛИ ЗАПРОС НЕВАЛИДЕН:
{
  "LlmName": "Имя_Модели",
  "IsValidPrompt": false,
  "ReasonIfInvalid": "Конкретная причина, почему запрос невалиден (на русском языке)",
  "Definitions": [],
  "Values": []
}

User:
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


    ResponseDtoBase? responsePart = ResponseDtoBase.FromString(line);

    Console.WriteLine(responsePart?.SerializeMultiLine() ?? "Error: response part is null");
}
