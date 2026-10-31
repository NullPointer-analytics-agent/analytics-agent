using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SharedClasses.ResponseDto.BehaviourModel;

namespace LlmService;

public class PromptParameters
{
    public OpenAIPromptExecutionSettings PromptExecutionSettings => new()
    {
        ResponseFormat = typeof(BehaviourModelDto)
    };

    public void AddSystemPromptsToHistory(ChatHistory history)
    {
        history.AddSystemMessage(GetFullSystemPrompt());
    }

    public string GetFullSystemPrompt() => $"""
    {BasicRules}
    
    {ReasoningRules}
    
    {DefinitionsRules}
    
    {InitialStateRules}

    {ExpressionRules}
    """;

    private string BasicRules => """
    Пользователь присылает набор состояний и начальное состояние некоторое логическое выражение.
    Сначала описать резульат естественным языков в Reasoning а потом заполнить строгие поля.
    Правила заполнения строгих полей указаны ниже.
    """;


    private string ReasoningRules => """
    Опиши в поле Reasoning подробно (на естественном языке) то что в дальнейшем запишешь в строгий формат
    """;

    private string DefinitionsRules => """
    Definitions:
        string VariableName - имя переменной в PascalCase, у каждой переменной уникальное
        TypeOfVariable VarType - тип (Integer, Boolean, Enum)
        int? IntegerMin - нижняя граница для целого числа. Заполняется только если тип Integer, иначе null
        int? IntegerMax - верхняя граница для целого числа. Заполняется только если тип Integer, иначе null
        List<string>? EnumValues - набор значений для enum. Заполняется только если тип Enum, иначе null
    """;

    private string InitialStateRules => """
    InitialStates:
        string VariableName - имя переменной к которой относится состояние. Сама переменная должна быть определена в Definitions.
        int? IntegerValue - Значение для переменной типа Integer. Обязано быть заполнено для Integer (быть зажато в указанном ранее диапазоне Definition), в проитвном случае быть null
        string? EnumValue - Значение для переменной типа Enum. Обязано быть заполнено для Enum (соответсвовать одному из доступных значений Definition), в проитвном случае быть null
        bool? BooleanValue - Значение для переменной типа Beolean. Обязано быть заполнено для Boolean, в проитвном случае быть null
    """;


    private string ExpressionRules => """
    Expression:
        ExpressionElementType NodeType - тип узла дерева выражения (переменная, конкретное значение или логическая/математическая операция).
        ExpressionElement? BinaryLeftChild - левый потомок выражения. Заполняется только для бинарных операций (And, Or, Equal, Greater, Less, GreaterOrEqual, LessOrEqual), иначе null.
        ExpressionElement? BinaryRightChild - правый потомок выражения. Заполняется только для бинарных операций (And, Or, Equal, Greater, Less, GreaterOrEqual, LessOrEqual), иначе null.
        ExpressionElement? UnaryChild - единственный потомок выражения. Заполняется только для унарных операций (Not), иначе null.
        string? VariableName - имя переменной. Заполняется только если NodeType равен Variable. Сама переменная обязана быть определена в Definitions. В противном случае null.
        VariableValue? Value - конкретное значение. Заполняется только если NodeType равен Value. Должно соответствовать правилам заполнения из InitialStates (но без привязки к конкретному VariableName). В противном случае null.
    """;
}