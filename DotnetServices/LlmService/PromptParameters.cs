using Microsoft.SemanticKernel.Connectors.OpenAI;
using SharedClasses.ResponseDto;

namespace LlmService;

public class PromptParameters
{
    public OpenAIPromptExecutionSettings PromptExecutionSettings => new()
    {
        ResponseFormat = typeof(BehaviourModelDto),
        Temperature = 0.2f
    };

    public string SystemPrompt { get; } = """
    System: Твоя задача — проанализировать запрос пользователя, определить его валидность и извлечь переменные состояния.

    ОСНОВНЫЕ ПРАВИЛА:
    1. Имя модели (LlmName): Укажи свое реальное коммерческое название (например: "Qwen", "GPT-4o", "DeepSeek-R1"). Не используй заглушки.
    2. Язык идентификаторов (VariableName): Имена всех переменных пиши строго на английском языке в стиле CamelCase или snake_case (например: 'carSpeed', 'weatherCondition', 'isDaylight').
    3. Значения Enum (EnumValues): Пиши элементы перечислений на английском языке. Поле заполняется ТОЛЬКО если VarType == 'Enum', иначе должно быть null.

    КРИТЕРИИ ВАЛИДНОСТИ (IsValidPrompt):
    - Выстави true, если пользователь четко описал список явлений/состояний И указал их начальную конфигурацию (исходные значения).
    - Выстави false, если запрос не по теме (просьбы посчитать, написать код, рассказать фильм) или в тексте не хватает данных для инициализации значений. В этом случае обязательно заполни поле 'ReasonIfInvalid' конкретной причиной на русском языке. Для валидных запросов оставь его пустым.
    
    """;
}
