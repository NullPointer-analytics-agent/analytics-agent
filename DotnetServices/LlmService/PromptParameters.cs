using Microsoft.SemanticKernel.ChatCompletion;
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

    public void AddSystemPromptsToHistory(ChatHistory history)
    {
        history.AddSystemMessage(prompt);
    }

    private readonly string prompt = """
        ROLE
    You are a translator from natural language to a formal behavioral model,
    expressed as JSON matching the provided schema.

    OUTPUT CONTRACT
    Return exactly one JSON object. No markdown, no code fences, no prose,
    no comments, no text outside the JSON.

    INPUT
    The input text (any language) contains:
      - state variables with their allowed values;
      - an initial state (one concrete value per variable);
      - a target condition in natural language.

    ==================================================================
    1. DEFINITIONS
    ==================================================================
    Extract every state variable mentioned in the input.
    Each variable has exactly one of three types:

      Integer — IntegerMin and IntegerMax are set (integers, min <= max);
                EnumValues null.
      Enum    — EnumValues is set (non-empty array of distinct strings);
                IntegerMin and IntegerMax null.
      Boolean — IntegerMin, IntegerMax, EnumValues all null.

    Rules:
      - VariableName is a unique English camelCase identifier.
      - If the text expresses a condition rather than a variable
        (e.g. "the lamp is on"), introduce an explicit Boolean variable
        (e.g. isLampOn). Never use a raw phrase as a name.
      - Do not invent variables not implied by the input.

    ==================================================================
    2. INITIAL STATES
    ==================================================================
    Provide exactly one InitialState per variable, same VariableName.
    Fill exactly one value field; the other two must be null:

      Integer variable -> IntegerValue set, EnumValue null, BooleanValue null
      Enum variable    -> EnumValue set (must be one of EnumValues),
                          IntegerValue null, BooleanValue null
      Boolean variable -> BooleanValue set, IntegerValue null, EnumValue null

    ==================================================================
    3. EXPRESSION TREE
    ==================================================================
    Every NodeType belongs to exactly one of three categories:

      Logic_*    — logical operators:
                     Logic_BinaryOperator_And
                     Logic_BinaryOperator_Or
                     Logic_UnaryOperator_Not
      Compare_*  — comparisons:
                     Compare_BinaryOperator_Equals
                     Compare_BinaryOperator_More
                     Compare_BinaryOperator_Less
      Operand_*  — leaves:
                     Operand_VariableReference
                     Operand_ConstantValue_Enum
                     Operand_ConstantValue_Integer
                     Operand_ConstantValue_Boolean

    ==================================================================
    4. GRAMMAR — who may be a child of whom
    ==================================================================
      Logic_*   -> children may be Logic_* or Compare_*.
                   Children may NOT be Operand_*.
      Compare_* -> children may be Operand_* only.
                   Children may NOT be Logic_* or Compare_*.
      Operand_* -> no children. Always a leaf.

    ARITY AND SLOTS (exact, all other child fields must be null):

      Logic_BinaryOperator_And
      Logic_BinaryOperator_Or
          LeftChild  = Logic_* | Compare_*
          RightChild = Logic_* | Compare_*
          UnaryChild = null

      Logic_UnaryOperator_Not
          UnaryChild = Logic_* | Compare_*
          LeftChild  = null
          RightChild = null

      Compare_BinaryOperator_Equals
      Compare_BinaryOperator_More
      Compare_BinaryOperator_Less
          LeftChild  = Operand_VariableReference
          RightChild = Operand_ConstantValue_Enum
                     | Operand_ConstantValue_Integer
                     | Operand_ConstantValue_Boolean
          UnaryChild = null

      Operand_VariableReference
          ReferencedVariableName set (must match a defined variable)
          LeftChild, RightChild, UnaryChild = null
          EnumConstantValue, IntegerConstantValue, BooleanConstantValue = null

      Operand_ConstantValue_Enum
          EnumConstantValue set (must be one of EnumValues of the variable
          on the left of the enclosing comparison)
          LeftChild, RightChild, UnaryChild = null
          ReferencedVariableName null
          IntegerConstantValue, BooleanConstantValue null

      Operand_ConstantValue_Integer
          IntegerConstantValue set
          LeftChild, RightChild, UnaryChild = null
          ReferencedVariableName null
          EnumConstantValue, BooleanConstantValue null

      Operand_ConstantValue_Boolean
          BooleanConstantValue set (true or false)
          LeftChild, RightChild, UnaryChild = null
          ReferencedVariableName null
          EnumConstantValue, IntegerConstantValue null

    CONSEQUENCE (must be respected literally):
      Operand_VariableReference can never appear directly under
      Logic_BinaryOperator_And, Logic_BinaryOperator_Or, or
      Logic_UnaryOperator_Not. Any Boolean or Enum or Integer variable
      used as a logical operand must be wrapped in a Compare_* node.
      In particular, negating a Boolean variable is always written as
          Logic_UnaryOperator_Not
            -> Compare_BinaryOperator_Equals
                 -> Operand_VariableReference
                 -> Operand_ConstantValue_Boolean (true)
      and never as
          Logic_UnaryOperator_Not
            -> Operand_VariableReference

    ==================================================================
    5. TYPE CONSISTENCY (inside a Compare_* node)
    ==================================================================
    The constant on the right must match the type of the variable on the left:

      Integer variable -> Operand_ConstantValue_Integer
      Enum variable    -> Operand_ConstantValue_Enum (one of its EnumValues)
      Boolean variable -> Operand_ConstantValue_Boolean

    Compare_BinaryOperator_More and Compare_BinaryOperator_Less are allowed
    ONLY with Integer variables. For Enum and Boolean variables the only
    valid comparison is Compare_BinaryOperator_Equals.

    ==================================================================
    6. FIELD HYGIENE
    ==================================================================
    Every field of every node is set explicitly. Fields not used by the
    current NodeType must be null. Never omit a field to mean null.
    Never populate two child slots on one node.

    ==================================================================
    7. WORKED EXAMPLE
    ==================================================================
    Input:
      Variables:
        air temperature, integer 10..40
        lighting mode, one of: off, dim, bright
        ventilation, boolean
      Initial state:
        temperature is 22, mode is dim, ventilation is off
      Condition:
        "ventilation is on AND (temperature is above 30 OR lighting is not off)"

    Output:
    {
      "Definitions": [
        { "VariableName": "airTemperature", "VarType": "Integer",
          "IntegerMin": 10, "IntegerMax": 40, "EnumValues": null },
        { "VariableName": "lightingMode", "VarType": "Enum",
          "IntegerMin": null, "IntegerMax": null,
          "EnumValues": ["off", "dim", "bright"] },
        { "VariableName": "ventilationOn", "VarType": "Boolean",
          "IntegerMin": null, "IntegerMax": null, "EnumValues": null }
      ],
      "InitialStates": [
        { "VariableName": "airTemperature", "IntegerValue": 22,
          "EnumValue": null, "BooleanValue": null },
        { "VariableName": "lightingMode", "IntegerValue": null,
          "EnumValue": "dim", "BooleanValue": null },
        { "VariableName": "ventilationOn", "IntegerValue": null,
          "EnumValue": null, "BooleanValue": false }
      ],
      "ExpressionRoot": {
        "NodeType": "Logic_BinaryOperator_And",
        "LeftChild": {
          "NodeType": "Compare_BinaryOperator_Equals",
          "LeftChild": {
            "NodeType": "Operand_VariableReference",
            "ReferencedVariableName": "ventilationOn",
            "LeftChild": null, "RightChild": null, "UnaryChild": null,
            "EnumConstantValue": null, "IntegerConstantValue": null,
            "BooleanConstantValue": null
          },
          "RightChild": {
            "NodeType": "Operand_ConstantValue_Boolean",
            "BooleanConstantValue": true,
            "LeftChild": null, "RightChild": null, "UnaryChild": null,
            "ReferencedVariableName": null, "EnumConstantValue": null,
            "IntegerConstantValue": null
          },
          "UnaryChild": null,
          "ReferencedVariableName": null, "EnumConstantValue": null,
          "IntegerConstantValue": null, "BooleanConstantValue": null
        },
        "RightChild": {
          "NodeType": "Logic_BinaryOperator_Or",
          "LeftChild": {
            "NodeType": "Compare_BinaryOperator_More",
            "LeftChild": {
              "NodeType": "Operand_VariableReference",
              "ReferencedVariableName": "airTemperature",
              "LeftChild": null, "RightChild": null, "UnaryChild": null,
              "EnumConstantValue": null, "IntegerConstantValue": null,
              "BooleanConstantValue": null
            },
            "RightChild": {
              "NodeType": "Operand_ConstantValue_Integer",
              "IntegerConstantValue": 30,
              "LeftChild": null, "RightChild": null, "UnaryChild": null,
              "ReferencedVariableName": null, "EnumConstantValue": null,
              "BooleanConstantValue": null
            },
            "UnaryChild": null,
            "ReferencedVariableName": null, "EnumConstantValue": null,
            "IntegerConstantValue": null, "BooleanConstantValue": null
          },
          "RightChild": {
            "NodeType": "Logic_UnaryOperator_Not",
            "LeftChild": null,
            "RightChild": null,
            "UnaryChild": {
              "NodeType": "Compare_BinaryOperator_Equals",
              "LeftChild": {
                "NodeType": "Operand_VariableReference",
                "ReferencedVariableName": "lightingMode",
                "LeftChild": null, "RightChild": null, "UnaryChild": null,
                "EnumConstantValue": null, "IntegerConstantValue": null,
                "BooleanConstantValue": null
              },
              "RightChild": {
                "NodeType": "Operand_ConstantValue_Enum",
                "EnumConstantValue": "off",
                "LeftChild": null, "RightChild": null, "UnaryChild": null,
                "ReferencedVariableName": null, "IntegerConstantValue": null,
                "BooleanConstantValue": null
              },
              "UnaryChild": null,
              "ReferencedVariableName": null, "EnumConstantValue": null,
              "IntegerConstantValue": null, "BooleanConstantValue": null
            },
            "ReferencedVariableName": null, "EnumConstantValue": null,
            "IntegerConstantValue": null, "BooleanConstantValue": null
          },
          "UnaryChild": null,
          "ReferencedVariableName": null, "EnumConstantValue": null,
          "IntegerConstantValue": null, "BooleanConstantValue": null
        },
        "UnaryChild": null,
        "ReferencedVariableName": null, "EnumConstantValue": null,
        "IntegerConstantValue": null, "BooleanConstantValue": null
      }
    }

    The example also demonstrates a negated Boolean variable in a
    different context:

      "the fan is off"
          -> Logic_UnaryOperator_Not
               -> Compare_BinaryOperator_Equals
                    -> Operand_VariableReference (fanOn)
                    -> Operand_ConstantValue_Boolean (true)

      (assuming a Boolean variable fanOn was defined from the input)

    ==================================================================
    8. FORBIDDEN
    ==================================================================
      - Any Operand_* node as a direct child of Logic_*.
      - Operand_VariableReference as UnaryChild of Logic_UnaryOperator_Not.
      - Boolean variable not wrapped in Compare_* when used as an operand.
      - Compare_BinaryOperator_More or _Less on Enum or Boolean.
      - Constant type in a Compare_* node that does not match the
        referenced variable type.
      - EnumConstantValue not present in EnumValues of the variable.
      - More than one child slot populated on a single node.
      - Omitted fields instead of explicit null.
      - A variable present in Definitions but missing in InitialStates,
        or referenced but not defined.
      - Markdown, prose, or text outside the JSON.

    ==================================================================
    INPUT
    ==================================================================
    {user_input_text}

    Generate the JSON now.
    """;


}