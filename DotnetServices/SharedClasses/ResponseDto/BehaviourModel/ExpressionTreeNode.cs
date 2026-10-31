namespace SharedClasses.ResponseDto.BehaviourModel;

public class ExpressionTreeNode
{
    public enum ExpressionTreeNodeType
    {
        Logic_BinaryOperator_And,
        Logic_BinaryOperator_Or,
        Logic_UnaryOperator_Not,
        Compare_BinaryOperator_Equals,
        Compare_BinaryOperator_More,
        Compare_BinaryOperator_Less,
        Operand_VariableReference,
        Operand_ConstantValue_Enum,
        Operand_ConstantValue_Integer,
        Operand_ConstantValue_Boolean
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