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

    public string SystemPrompt { get; } = """
    Твоя задача — проанализировать запрос пользователя и заполнить модель поведения.

    ## ПРАВИЛА ИМЕНОВАНИЯ
    - VariableName: английский язык, camelCase (например: carSpeed, weatherCondition, isDaylight).
    - EnumValues: английский язык, нижний регистр (например: sunny, cloudy, rainy).
    - LlmName: укажи свое реальное название (например, "Qwen2.5").

    ## ПРАВИЛА ЗАПОЛНЕНИЯ ЗНАЧЕНИЙ (Values)
    Для каждой переменной в списке Values заполни ТОЛЬКО ОДНО поле значения в зависимости от её типа (VarType):
    - Integer → заполни только IntegerCurrentValue (остальные сделай null).
    - Enum → заполни только EnumCurrentValue (значение должно строго совпадать с одним из EnumValues, остальные сделай null).
    - Boolean → заполни только BooleanCurrentValue (остальные сделай null).

    ## ПРАВИЛА ПОСТРОЕНИЯ ДЕРЕВА ВЫРАЖЕНИЙ (RootExpression)
    Дерево состоит из узлов ExpressionElement. Каждый узел имеет строго один Type. Заполняй поля СТРОГО по следующим правилам, все неиспользуемые поля делай null:

    1. Если Type = "Operand" (проверка: переменная == значение):
       - Заполни ТОЛЬКО OperandValue (укажи VariableName и целевое значение).
       - BinaryLeftChild = null, BinaryRightChild = null, UnaryChild = null.

    2. Если Type = "UnaryOperatorNot" (проверка: переменная != значение):
       - Заполни ТОЛЬКО UnaryChild (внутри должен быть узел типа "Operand").
       - OperandValue = null, BinaryLeftChild = null, BinaryRightChild = null.
       - ВАЖНО: Используй ТОЛЬКО если в тексте есть явное отрицание ("не", "нет", "не равно"). Например: "не светло", "не дождь", "скорость не равна 40".

    3. Если Type = "BinaryOperatorAnd" (логическое И):
       - Заполни ТОЛЬКО BinaryLeftChild и BinaryRightChild (любые узлы выражения).
       - OperandValue = null, UnaryChild = null.

    4. Если Type = "BinaryOperatorOr" (логическое ИЛИ):
       - Заполни ТОЛЬКО BinaryLeftChild и BinaryRightChild (любые узлы выражения).
       - OperandValue = null, UnaryChild = null.

    ## ПРАВИЛА ИНТЕРПРЕТАЦИИ УСЛОВИЙ
    - "светло" → Operand с BooleanCurrentValue = true (НЕ используй UnaryOperatorNot)
    - "не светло" → UnaryOperatorNot с Operand внутри (BooleanCurrentValue = true)
    - "дождь" → Operand с EnumCurrentValue = "rainy"
    - "не дождь" → UnaryOperatorNot с Operand внутри (EnumCurrentValue = "rainy")
    - "скорость равна 40" → Operand с IntegerCurrentValue = 40
    - "скорость не равна 40" → UnaryOperatorNot с Operand внутри (IntegerCurrentValue = 40)

    ## КРИТЕРИИ ВАЛИДНОСТИ (IsValidPrompt)
    - Выстави true, если пользователь описал явления, их начальные значения и логическое условие.
    - Выстави false, если запрос не по теме или не хватает данных. В этом случае обязательно заполни ReasonIfInvalid на русском языке.
    """;
}
