namespace SharedClasses.ResponseDto.BehaviourModel;

public class ExpressionTreeNode
{
    public enum ExpressionTreeNodeType
    {
        LogicOperator_BinaryOperator_And,
        LogicOperator_BinaryOperator_Or,
        LogicOperator_UnaryOperator_Not,
        CompareOperator_BinaryOperator_Equals,
        CompareOperator_BinaryOperator_More,
        CompareOperator_BinaryOperator_Less,
        Operand_VariableReference,
        Operand_ConstantValue
    }

    public ExpressionTreeNodeType NodeType { get; set; }
    
    public ExpressionTreeNode? LeftChild { get; set; }
    public ExpressionTreeNode? RightChild { get; set; }
    public ExpressionTreeNode? UnaryChild { get; set; }



    public string? ReferencedVariableName { get; set; }

    public string? EnumConstantValue { get; set; }
    public int? IntegerConstantValue { get; set; }
    public bool? BooleanConstantValue { get; set; }

}