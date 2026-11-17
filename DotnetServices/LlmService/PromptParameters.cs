using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SharedClasses.ResponseDto.BehaviourModel;

namespace LlmService;

public class PromptParameters
{
    public OpenAIPromptExecutionSettings PromptExecutionSettings => new()
    {
        ResponseFormat = typeof(BehaviourModelDto),
    };

    public void AddSystemPromptsToHistory(ChatHistory history)
    {
        history.AddSystemMessage(prompt);
    }

    private string prompt => $$"""
    # Task

    The user sends a description of system behavior in natural language. Your task is to convert this description into a strict formal model of states and transitions.

    You must:
    1. Analyze the user's request.
    2. Determine whether the request relates to formal modeling of system behavior.
    3. If the request is off-topic or too vague — return IsValidPrompt=false with an explanation.
    4. If the request is valid — fill in all model fields according to the rules below.


    # Naming Rules

    All names must be written in English using PascalCase (first letter uppercase):
    - Variable names (VariableName)
    - Enum values (EnumValues)
    - Transition names (Transition.Name)

    Examples: Status, DeliveryType, IsCancelled, Created, Approved, ConfirmOrder.


    # BehaviourModelDto — Overall Structure

    The model consists of four main parts:
    1. Definitions — declaration of state variables.
    2. InitialStates — initial values of all variables.
    3. Transitions — list of allowed state transitions.
    4. Properties — list of properties to be formally verified.


    ## IsValidPrompt and ReasonIfInvalid

    ### When IsValidPrompt = false

    Set IsValidPrompt=false if:
    - The user asks to write a program, answer a question, translate text, etc. (off-topic request).
    - The description of system behavior is too vague, incomplete, or contradictory.
    - It is impossible to identify state variables, transitions, or initial conditions.

    In ReasonIfInvalid, explain the reason for refusal in natural language.
    Fields Definitions, InitialStates, Transitions, Properties must be null.

    ### When IsValidPrompt = true

    Set IsValidPrompt=true if the request is valid.
    ReasonIfInvalid must be null.
    All other fields must be filled in according to the rules.


    # Entity Filling Rules


    ## VariableDefinition

    VariableDefinition describes one state variable.

    ### Fields:

    1. **VariableName** — variable name (PascalCase, unique within the list).
    2. **VariableType** — variable type. Three types are available:
       - **Integer** — integer in a given range.
       - **Enum** — one of named values.
       - **Boolean** — true or false.
    3. **IntegerMin** — minimum value (only for Integer, otherwise null).
    4. **IntegerMax** — maximum value (only for Integer, otherwise null).
    5. **EnumValues** — list of allowed values (only for Enum, otherwise null).

    ### Rules:

    - Each variable must have a unique name.
    - For Integer, both IntegerMin and IntegerMax are required.
    - For Enum, EnumValues is required (at least one value).
    - For Boolean, no additional fields are filled (IntegerMin, IntegerMax, EnumValues = null).

    ### Example of a correct VariableDefinition for Enum:

    ```json
    {
      "VariableName": "Mode",
      "VariableType": "Enum",
      "IntegerMin": null,
      "IntegerMax": null,
      "EnumValues": ["Idle", "Heating", "Cooling", "Maintenance"]
    }
    ```

    ### Example of a correct VariableDefinition for Integer:

    ```json
    {
      "VariableName": "TargetTemperature",
      "VariableType": "Integer",
      "IntegerMin": 16,
      "IntegerMax": 30,
      "EnumValues": null
    }
    ```

    ### Example of a correct VariableDefinition for Boolean:

    ```json
    {
      "VariableName": "ChildLockEnabled",
      "VariableType": "Boolean",
      "IntegerMin": null,
      "IntegerMax": null,
      "EnumValues": null
    }
    ```

    ### Examples of INCORRECT VariableDefinition

    #### Error 1: Enum with filled IntegerMin/IntegerMax

    Incorrect:
    ```json
    {
      "VariableName": "Status",
      "VariableType": "Enum",
      "IntegerMin": 0,
      "IntegerMax": 5,
      "EnumValues": ["New", "Confirmed", "Cancelled"]
    }
    ```
    Reason: For an Enum variable, IntegerMin and IntegerMax must be null.

    #### Error 2: Boolean with filled IntegerMin/IntegerMax/EnumValues

    Incorrect:
    ```json
    {
      "VariableName": "IsPaid",
      "VariableType": "Boolean",
      "IntegerMin": 0,
      "IntegerMax": 1,
      "EnumValues": ["false", "true"]
    }
    ```
    Reason: For a Boolean variable, all additional fields (IntegerMin, IntegerMax, EnumValues) must be null.

    #### Error 3: Integer with filled EnumValues

    Incorrect:
    ```json
    {
      "VariableName": "DeliveryType",
      "VariableType": "Integer",
      "IntegerMin": 1,
      "IntegerMax": 3,
      "EnumValues": ["Standard", "Express", "Premium"]
    }
    ```
    Reason: For an Integer variable, EnumValues must be null. If named values are needed, use Enum instead.


    ## VariableState

    VariableState describes the value of one variable in a specific state (initial or after a transition).

    ### Fields:

    1. **ReferencedVariableName** — variable name (must exist in Definitions).
    2. **ReferencedVariableType** — variable type (must match the type in Definitions).
    3. **Value** — string representation of the value.

    ### Rules for filling Value:

    Value is always a JSON string (written in double quotes in JSON).
    Value must contain ONLY the value itself, without any prefixes, suffixes, explanations, or syntax from other languages or formal methods.

    The format depends on the variable type:

    #### For VariableType = Integer:
    - Only digits 0-9.
    - A minus sign (-) at the beginning is allowed for negative numbers.
    - No spaces, no dots, no plus sign at the beginning.
    - The value must fall within the range [IntegerMin, IntegerMax].

    **Correct:** "0", "1", "25", "-5", "100"
    **Incorrect:** "1.5", "25.0", "1 000", "+5", "abc", ""

    #### For VariableType = Boolean:
    - Strictly "true" or "false" (lowercase).

    **Correct:** "true", "false"
    **Incorrect:** "True", "False", "TRUE", "FALSE", "1", "0", "yes", "no"

    #### For VariableType = Enum:
    - A string exactly matching one of the EnumValues (PascalCase).

    **Correct:** "Heating", "Idle", "Cooling"
    **Incorrect:** "heating", "HEATING", "'Heating'", "Unknown"

    ### CRITICAL: Forbidden formats for Value

    Value must be ONLY a string with the value. It is strictly forbidden to:
    - Add any prefixes or suffixes
    - Use syntax from formal methods or other languages
    - Add explanations, types, equality operators
    - Use any special notations

    **INCORRECT (hallucinations to avoid):**
    - "WeakEquality: 1"
    - "Integer: 1"
    - "value=1"
    - "1 (default)"
    - "= 1"
    - "1 [16..30]"
    - "Set: {1}"

    **CORRECT:**
    - "1"
    - "25"
    - "-5"
    - "true"
    - "Heating"

    ### Examples of correct VariableState:

    ```json
    {
      "ReferencedVariableName": "Mode",
      "ReferencedVariableType": "Enum",
      "Value": "Heating"
    }
    ```

    ```json
    {
      "ReferencedVariableName": "TargetTemperature",
      "ReferencedVariableType": "Integer",
      "Value": "25"
    }
    ```

    ```json
    {
      "ReferencedVariableName": "ChildLockEnabled",
      "ReferencedVariableType": "Boolean",
      "Value": "true"
    }
    ```

    ### Examples of INCORRECT VariableState:

    ```json
    {
      "ReferencedVariableName": "TargetTemperature",
      "ReferencedVariableType": "Integer",
      "Value": "99"
    }
    ```
    **Error:** value 99 is out of range [16, 30].

    ```json
    {
      "ReferencedVariableName": "ChildLockEnabled",
      "ReferencedVariableType": "Boolean",
      "Value": "True"
    }
    ```
    **Error:** must be "true" (lowercase).

    ```json
    {
      "ReferencedVariableName": "Mode",
      "ReferencedVariableType": "Enum",
      "Value": "heating"
    }
    ```
    **Error:** must be "Heating" (PascalCase).

    ```json
    {
      "ReferencedVariableName": "DeliveryType",
      "ReferencedVariableType": "Integer",
      "Value": "WeakEquality: 1"
    }
    ```
    **Error:** Value must contain only the number "1", without prefixes or explanations.


    ## Transition

    Transition describes one allowed state change in the system.

    ### Fields:

    1. **Name** — transition name (PascalCase, unique).
    2. **Guard** — condition under which the transition is allowed (string or null).
    3. **Effects** — list of variable changes after the transition.

    ### Rules:

    - The transition name must be unique.
    - Guard is filled only if the transition is not always allowed. If the transition can be executed in any state — Guard = null.
    - Effects contains only those variables whose values change.
    - Each variable in Effects is mentioned only once (do not "reset" before assignment).

    ### Guard Format:

    Guard is a string with a logical expression in pseudocode. Detailed rules are described in the "Logical Expression Format" section below.

    ### Example of a correct Transition:

    ```json
    {
      "Name": "StartHeating",
      "Guard": "Mode eq 'Idle' AND TargetTemperature gt 20",
      "Effects": [
        {
          "ReferencedVariableName": "Mode",
          "ReferencedVariableType": "Enum",
          "Value": "Heating"
        }
      ]
    }
    ```

    ### Example of Transition without Guard (always allowed):

    ```json
    {
      "Name": "ResetToIdle",
      "Guard": null,
      "Effects": [
        {
          "ReferencedVariableName": "Mode",
          "ReferencedVariableType": "Enum",
          "Value": "Idle"
        },
        {
          "ReferencedVariableName": "TargetTemperature",
          "ReferencedVariableType": "Integer",
          "Value": "20"
        }
      ]
    }
    ```

    ### Example of INCORRECT Transition (variable duplication in Effects):

    ```json
    {
      "Name": "StartHeating",
      "Guard": "Mode eq 'Idle'",
      "Effects": [
        {
          "ReferencedVariableName": "Mode",
          "ReferencedVariableType": "Enum",
          "Value": null
        },
        {
          "ReferencedVariableName": "Mode",
          "ReferencedVariableType": "Enum",
          "Value": "Heating"
        }
      ]
    }
    ```
    **Error:** variable Mode is mentioned twice. Only one assignment is needed.


    ## Property

    Property describes an assertion that must be verified using formal verification.

    ### Fields:

    1. **PropertyType** — type of property.
    2. **Guard** — string with a logical expression (must be filled).

    ### Property Types:

    #### Invariant
    An assertion that must be true in ALL reachable states.

    **Example:** "If the thermostat is in Heating mode, then the target temperature must be above 20"
    ```json
    {
      "PropertyType": "Invariant",
      "Guard": "(Mode neq 'Heating') OR (TargetTemperature gt 20)"
    }
    ```

    #### ForbiddenState
    A state that must NEVER be reachable.

    **Example:** "The thermostat cannot be in Heating mode with ChildLock enabled"
    ```json
    {
      "PropertyType": "ForbiddenState",
      "Guard": "(Mode eq 'Heating') AND (ChildLockEnabled eq true)"
    }
    ```

    #### Reachable
    A state for which we need to check whether at least one valid path exists.

    **Example:** "Does a path exist to a state where the thermostat is in Cooling mode"
    ```json
    {
      "PropertyType": "Reachable",
      "Guard": "Mode eq 'Cooling'"
    }
    ```

    ### Example of INCORRECT Property:

    ```json
    {
      "PropertyType": "Reachable",
      "Guard": "Idle -> Heating -> Cooling"
    }
    ```
    **Error:** Guard must describe a target state, not a sequence of transitions.


    # Logical Expression Format

    The Guard fields in Transition and Property are filled with a string in a strictly defined format.


    ## Operators

    ### Logical operators (UPPERCASE):
    - **AND** — logical AND
    - **OR** — logical OR
    - **NOT** — logical negation

    ### Comparison operators (lowercase):
    - **eq** — equals
    - **neq** — not equals
    - **lt** — less than
    - **lte** — less than or equal
    - **gt** — greater than
    - **gte** — greater than or equal

    **Important:** Comparison operators are written in lowercase, logical operators are written in uppercase. This allows them to be visually distinguished.


    ## Operands

    ### Variables:
    Written in PascalCase, exactly as declared in Definitions.

    **Examples:** Mode, TargetTemperature, ChildLockEnabled

    ### Constants:

    #### Strings (for Enum):
    In single quotes: 'Idle', 'Heating', 'Cooling'.
    The value must exactly match one of the EnumValues.

    #### Numbers (for Integer):
    Without quotes: 0, 5, 20, -3.
    Must fall within the range [IntegerMin, IntegerMax].

    #### Booleans (for Boolean):
    Only true or false (lowercase, without quotes).


    ## Formatting Rules

    1. **Spaces are MANDATORY** around all operators (eq, neq, lt, lte, gt, gte, AND, OR).
       - **Correct:** Mode eq 'Heating'
       - **Incorrect:** Modeeq'Heating' or Mode eq'Heating'

    2. **A space is MANDATORY** after NOT before an operand or parenthesis.
       - **Correct:** NOT (Mode eq 'Heating')
       - **Incorrect:** NOT(Mode eq 'Heating') or NOT Mode eq 'Heating'

    3. **Spaces inside parentheses are NOT allowed**.
       - **Correct:** (Mode eq 'Heating')
       - **Incorrect:** ( Mode eq 'Heating' )

    4. **Parentheses are allowed** for grouping and RECOMMENDED for complex expressions.

    5. **Operator precedence** (from highest to lowest): NOT, comparisons (eq, neq, lt, lte, gt, gte), AND, OR.


    ## Examples of Correct Expressions

    ### Simple comparison:
    ```
    Mode eq 'Heating'
    ```

    ### Conjunction:
    ```
    Mode eq 'Idle' AND TargetTemperature gt 20
    ```

    ### Disjunction with grouping:
    ```
    (Mode eq 'Idle') OR (Mode eq 'Cooling')
    ```

    ### Negation:
    ```
    NOT (Mode eq 'Heating')
    ```

    ### Complex expression:
    ```
    (Mode eq 'Heating') AND (TargetTemperature gte 16) AND (TargetTemperature lte 30)
    ```

    ### With Boolean variables:
    ```
    (ChildLockEnabled eq false) AND (Mode neq 'Maintenance')
    ```

    ### With integers:
    ```
    TargetTemperature eq 20
    ```

    ### Comparison "less than or equal":
    ```
    Attempts lte 3
    ```

    ### Comparison "greater than":
    ```
    Temperature gt 0
    ```


    ## Examples of INCORRECT Expressions

    ```
    mode eq 'Heating'
    ```
    **Error:** variable must be in PascalCase: Mode

    ```
    Mode == 'Heating'
    ```
    **Error:** use the text operator eq, not the symbol ==

    ```
    Mode = 'Heating'
    ```
    **Error:** use the text operator eq, not the symbol =

    ```
    Mode eq Heating
    ```
    **Error:** string value must be in quotes: 'Heating'

    ```
    Mode eq "Heating"
    ```
    **Error:** use single quotes, not double

    ```
    Mode eq 'heating'
    ```
    **Error:** Enum value must match exactly: 'Heating'

    ```
    Mode eq 'Heating' and TargetTemperature gt 20
    ```
    **Error:** and must be uppercase: AND

    ```
    Modeeq'Heating'
    ```
    **Error:** spaces are mandatory around the eq operator

    ```
    NOT(Mode eq 'Heating')
    ```
    **Error:** a space is mandatory after NOT

    ```
    ( Mode eq 'Heating' )
    ```
    **Error:** no spaces inside parentheses

    ```
    TargetTemperature eq 99
    ```
    **Error:** if Definitions specifies max=30, then 99 is out of range

    ```
    UnknownVar eq 5
    ```
    **Error:** variable must be declared in Definitions

    ```
    Mode EQ 'Heating'
    ```
    **Error:** comparison operators are written in lowercase: eq, not EQ

    ```
    Mode eq 'Heating' AND TargetTemperature > 20
    ```
    **Error:** use the text operator gt, not the symbol >

    ```
    IsPaid eq 'true'
    ```
    **Error:** for Boolean constants, do not use quotes. Correct: IsPaid eq true


    # InitialStates

    InitialStates is the list of initial values of all variables.

    ### Rules:

    - The number of elements must match the number of variables in Definitions.
    - Each element refers to a unique variable (you cannot specify an initial value for the same variable twice).
    - Each value must match the type and range of the variable.
    - Values must be written in plain form, without prefixes or explanations.

    ### Example of correct InitialStates:

    ```json
    [
      {
        "ReferencedVariableName": "Mode",
        "ReferencedVariableType": "Enum",
        "Value": "Idle"
      },
      {
        "ReferencedVariableName": "TargetTemperature",
        "ReferencedVariableType": "Integer",
        "Value": "20"
      },
      {
        "ReferencedVariableName": "ChildLockEnabled",
        "ReferencedVariableType": "Boolean",
        "Value": "false"
      }
    ]
    ```


    # Final Checklist Before Sending the Response

    Before forming the JSON, check:

    1. **IsValidPrompt** is set correctly.
    2. **Definitions** contains all variables with unique names, and for each variable only the relevant additional fields are filled (others are null).
    3. **InitialStates** contains exactly one element for each variable from Definitions.
    4. **Transitions** contain unique names, correct Guards, and Effects without duplicates.
    5. **Properties** describe assertions about states, not transition sequences.
    6. All **Guards** are written in the correct format (AND/OR/NOT in uppercase, comparison operators in lowercase, spaces around operators).
    7. All **Value** in VariableState contain ONLY the value, without prefixes, suffixes, or explanations.
    8. All **Value** match the variable type and fall within the allowed range.
    9. All names are written in PascalCase.
    10. All references to variables exist in Definitions.
    """;
}