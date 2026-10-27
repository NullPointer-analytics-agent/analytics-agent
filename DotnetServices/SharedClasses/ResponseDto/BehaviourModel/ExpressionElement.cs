namespace SharedClasses.ResponseDto.BehaviourModel;


public enum ExpressionElementType
{
    BinaryOperatorAnd, BinaryOperatorOr, UnaryOperatorNot, Operand
}

public class ExpressionElement
{
    public ExpressionElementType Type { get; set; }
    public VariableValue? OperandValue { get; set; }
    public ExpressionElement? BinaryLeftChild { get; set; }
    public ExpressionElement? BinaryRightChild { get; set; }
    public ExpressionElement? UnaryChild { get; set; }

}
