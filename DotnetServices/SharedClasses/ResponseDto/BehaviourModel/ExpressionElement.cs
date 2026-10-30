namespace SharedClasses.ResponseDto.BehaviourModel;


public enum ExpressionElementType
{
    Variable,
    Value,
    And,
    Or,
    Not,
    Equal,
}

public class ExpressionElement
{
    public ExpressionElementType Type { get; set; }

    public ExpressionElement? BinaryLeftChild { get; set; }
    public ExpressionElement? BinaryRightChild { get; set; }
    public ExpressionElement? UnaryChild { get; set; }

    public string? VariableName { get; set; }
    public VariableValue? Value { get; set; }

}
