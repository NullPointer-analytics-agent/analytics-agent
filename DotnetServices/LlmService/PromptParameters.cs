using Microsoft.SemanticKernel.Connectors.OpenAI;
using SharedClasses.ResponseDto.BehaviourModel;

namespace LlmService;

public class PromptParameters
{
    public OpenAIPromptExecutionSettings PromptExecutionSettings => new()
    {
        ResponseFormat = typeof(BehaviourModelDto),
        Temperature = 0.1f
    };

    public string BasicRules { get; } = """
    ## ЗАДАЧА
    1. Пользователь опишет явления (состояния) и логическое условие.
    2. В поле LlmName JSON-ответа укажи название модели (например, "Qwen", "Llama").
    3. Строго следуй инструкциям из других системных сообщений.
    """;

    public string DefinitionsRules { get; } = """
    ## ГЕНЕРАЦИЯ MASSIVE "Definitions"
    Выдели из блока «Явления» переменные и создай массив объектов по правилам:
    1. VariableName (string): перевод на английский, строго camelCase (пример: ballSize, floorType). Boolean-переменные начинай с is/has/can (пример: isOnFloor).
    2. VarType (string): строго одно из "Integer", "Enum", "Boolean".
    3. Заполнение полей по типам:
       - "Integer": IntegerMin и IntegerMax — целые числа (если границ нет, строго 0 и 100). EnumValues = null.
       - "Enum": EnumValues — массив строк на английском в нижнем регистре (пример: ["red", "blue"]). IntegerMin/Max = null.
       - "Boolean": IntegerMin, IntegerMax, EnumValues строго = null.
    """;

    public string ExpressionRules { get; } = """
    ## ГЕНЕРАЦИЯ ОБЪЕКТА "Expression" (Рекурсивное дерево условий)
    ### ЖЕСТКИЕ ПРАВИЛА СТРУКТУРЫ:
    1. КОРНЕМ может быть только оператор (And, Or, Not, Equal).
    2. Variable и Value — это всегда листья. Могут быть детьми ТОЛЬКО у узла Equal.
    3. Операторы And, Or, Not ссылаются ТОЛЬКО на другие операторы (Equal, And, Or, Not). Напрямую содержать Variable/Value они НЕ могут.
    4. Все неиспользуемые в типе узла поля строго = null.

    ### ЗАПОЛНЕНИЕ ПО ТИПАМ УЗЛОВ:
    1. Type: "Variable" -> Заполни VariableName (из Definitions). Остальные поля = null.
    2. Type: "Value" -> Заполни объект Value:
       - Для Integer: {"VariableType": "Integer", "IntegerValue": X, "EnumValue": null, "BooleanVaLue": null}
       - Для Enum: {"VariableType": "Enum", "IntegerValue": null, "EnumValue": "X", "BooleanVaLue": null}
       - Для Boolean: {"VariableType": "Boolean", "IntegerValue": null, "EnumValue": null, "BooleanVaLue": true/false}
       Остальные поля узла = null.
    3. Type: "Equal" -> Заполни BinaryLeftChild и BinaryRightChild (внутри них строго Variable или Value).
    4. Type: "And" / "Or" -> Заполни BinaryLeftChild и BinaryRightChild (внутри них Equal, And, Or или Not).
    5. Type: "Not" -> Заполни UnaryChild (внутри Equal, And, Or или Not).

    ### СОВМЕСТИМОСТЬ:
    - Equal сравнивает любые два значения одинакового типа.
    - And, Or, Not объединяют/инвертируют только Boolean-результаты других операторов.
    """;
}
