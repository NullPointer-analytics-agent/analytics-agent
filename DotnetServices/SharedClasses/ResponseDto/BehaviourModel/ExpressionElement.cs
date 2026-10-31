namespace SharedClasses.ResponseDto.BehaviourModel;


public enum ExpressionElementType
{
    Variable,
    Value,

    And,
    Or,

    Not,

    Equal,
    Greater,
    Less,
    GreaterOrEqual,
    LessOrEqual
}

public class ExpressionElement
{
    public ExpressionElementType NodeType { get; set; }
    public ExpressionElement? BinaryLeftChild { get; set; }
    public ExpressionElement? BinaryRightChild { get; set; }
    public ExpressionElement? UnaryChild { get; set; }
    public string? VariableName { get; set; }
    public VariableValue? Value { get; set; }

}
