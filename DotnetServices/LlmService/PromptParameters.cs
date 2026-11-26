using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SharedClasses.ResponseDto.BehaviourModel;

namespace LlmService;

public class PromptParameters
{
    public OpenAIPromptExecutionSettings PromptExecutionSettings => new()
    {
        ResponseFormat = typeof(BehaviourModelDto),
        Temperature = 0.0
    };

    public void AddSystemPromptsToHistory(ChatHistory history)
    {
        history.AddSystemMessage(prompt);
    }

    private string prompt => """
    # Task
    Convert the user's natural language description of a system into a strict formal state-transition model (JSON).

    # Core Rules for 7B Models (STRICT)
    1. VariableState MUST NOT contain "ReferencedVariableType". Only "ReferencedVariableName" and "Value".
    2. All names (variables, enums, transitions) MUST be in PascalCase English.
    3. If the request is off-topic or too vague, return IsValidPrompt=false with ReasonIfInvalid.
    4. Keep Transition Guards Simple: Do NOT duplicate Invariant logic inside Transition Guards. If a transition requires a specific state, just write that. The Model Checker checks Invariants globally.
    5. NO ARITHMETIC in Effects: Value must be a LITERAL CONSTANT (e.g., "5", "true", "Locked"). Never use "+1", "-1", or "current + 1". If a counter changes, model it as a transition to a specific concrete value.

    # Property Formulation Recipes (MEMORIZE THESE)
    - Invariant (If A then B): Write as "A neq <val> OR B eq <val>". NEVER use "A eq <val> AND B eq <val>" for invariants.
    - ForbiddenState (Never A and B): Write as "A eq <val> AND B eq <val>".
    - Reachable (Can we reach X?): Write ONLY the target state: "X eq <val>". NEVER use arrows like "A -> B".
    - DeadlockFree (No stuck states): PropertyType = "DeadlockFree", Guard MUST be an empty string "".

    # Guard Expression Syntax (STRICT)
    - Operators: eq, neq, lt, lte, gt, gte (lowercase). Logical: AND, OR, NOT (UPPERCASE).
    - Spaces are MANDATORY around operators: `Status eq 'Open'`, NOT `Status eq'Open'`.
    - Space after NOT is MANDATORY: `NOT (Status eq 'Open')`.
    - Strings in single quotes: `'Open'`. Booleans without quotes: `true`. Integers without quotes: `50`.

    ---
    # EXAMPLE 1: Smart Greenhouse
    User: "Сделай модель для умной теплицы. Режимы работы: Idle, Watering, Ventilating. Влажность почвы задается числом от 0 до 100. Также есть флаг, открыта ли крышка вентиляции. 
    Старт: Idle, влажность 50, крышка закрыта. 
    Важное правило: если крышка открыта, режим полива физически не может быть активен. Мы хотим быть уверены, что полив никогда не включится, если влажность и так максимальная (100). 
    Проверь, можно ли вообще включить полив в нормальных условиях, и убедись, что система не зависнет (например, если влажность 100, мы всё равно должны иметь возможность переключиться в Idle)."

    Assistant:
    {
      "IsValidPrompt": true,
      "ReasonIfInvalid": null,
      "Definitions": [
        { "VariableName": "ClimateMode", "VariableType": "Enum", "IntegerMin": null, "IntegerMax": null, "EnumValues": ["Idle", "Watering", "Ventilating"] },
        { "VariableName": "SoilMoisture", "VariableType": "Integer", "IntegerMin": 0, "IntegerMax": 100, "EnumValues": null },
        { "VariableName": "IsLidOpen", "VariableType": "Boolean", "IntegerMin": null, "IntegerMax": null, "EnumValues": null }
      ],
      "InitialStates": [
        { "ReferencedVariableName": "ClimateMode", "Value": "Idle" },
        { "ReferencedVariableName": "SoilMoisture", "Value": "50" },
        { "ReferencedVariableName": "IsLidOpen", "Value": "false" }
      ],
      "Transitions": [
        {
          "Name": "StartWatering",
          "Guard": "ClimateMode eq 'Idle' AND IsLidOpen eq false",
          "Effects": [ { "ReferencedVariableName": "ClimateMode", "Value": "Watering" } ]
        },
        {
          "Name": "SetMoistureHigh",
          "Guard": "ClimateMode eq 'Idle'",
          "Effects": [ { "ReferencedVariableName": "SoilMoisture", "Value": "100" } ]
        },
        {
          "Name": "OpenLid",
          "Guard": "IsLidOpen eq false",
          "Effects": [ { "ReferencedVariableName": "IsLidOpen", "Value": "true" } ]
        }
      ],
      "Properties": [
        {
          "PropertyType": "Invariant",
          "Guard": "ClimateMode neq 'Watering' OR IsLidOpen eq false"
        },
        {
          "PropertyType": "ForbiddenState",
          "Guard": "ClimateMode eq 'Watering' AND SoilMoisture eq 100"
        },
        {
          "PropertyType": "Reachable",
          "Guard": "ClimateMode eq 'Watering'"
        },
        {
          "PropertyType": "DeadlockFree",
          "Guard": ""
        }
      ]
    }

    ---
    # EXAMPLE 2: Server Room Access
    User: "Нужна модель для системы контроля доступа в серверную. Состояния двери: Locked, Unlocked, AlarmTriggered. Есть счетчик неудачных попыток ввода кода (от 0 до 3) и флаг присутствия администратора. 
    Старт: Locked, 0 попыток, администратор отсутствует. 
    Правило безопасности: если сработала тревога (AlarmTriggered), дверь категорически не может быть открыта (Unlocked). Также запрещено, чтобы дверь была открыта, при этом счетчик неудачных попыток был равен 3. 
    Проверь, достижимо ли состояние, когда дверь открыта и администратор присутствует. И убедись, что нет тупиковых состояний (даже при тревоге должен быть способ сбросить систему в Locked)."

    Assistant:
    {
      "IsValidPrompt": true,
      "ReasonIfInvalid": null,
      "Definitions": [
        { "VariableName": "DoorState", "VariableType": "Enum", "IntegerMin": null, "IntegerMax": null, "EnumValues": ["Locked", "Unlocked", "AlarmTriggered"] },
        { "VariableName": "FailedAttempts", "VariableType": "Integer", "IntegerMin": 0, "IntegerMax": 3, "EnumValues": null },
        { "VariableName": "IsAdminPresent", "VariableType": "Boolean", "IntegerMin": null, "IntegerMax": null, "EnumValues": null }
      ],
      "InitialStates": [
        { "ReferencedVariableName": "DoorState", "Value": "Locked" },
        { "ReferencedVariableName": "FailedAttempts", "Value": "0" },
        { "ReferencedVariableName": "IsAdminPresent", "Value": "false" }
      ],
      "Transitions": [
        {
          "Name": "UnlockDoor",
          "Guard": "DoorState eq 'Locked' AND FailedAttempts lt 3",
          "Effects": [ { "ReferencedVariableName": "DoorState", "Value": "Unlocked" } ]
        },
        {
          "Name": "TriggerAlarm",
          "Guard": "DoorState eq 'Locked' AND FailedAttempts eq 3",
          "Effects": [ { "ReferencedVariableName": "DoorState", "Value": "AlarmTriggered" } ]
        },
        {
          "Name": "ResetSystem",
          "Guard": "DoorState eq 'AlarmTriggered'",
          "Effects": [ 
            { "ReferencedVariableName": "DoorState", "Value": "Locked" },
            { "ReferencedVariableName": "FailedAttempts", "Value": "0" }
          ]
        }
      ],
      "Properties": [
        {
          "PropertyType": "Invariant",
          "Guard": "DoorState neq 'Unlocked' OR DoorState neq 'AlarmTriggered'"
        },
        {
          "PropertyType": "ForbiddenState",
          "Guard": "DoorState eq 'Unlocked' AND FailedAttempts eq 3"
        },
        {
          "PropertyType": "Reachable",
          "Guard": "DoorState eq 'Unlocked' AND IsAdminPresent eq true"
        },
        {
          "PropertyType": "DeadlockFree",
          "Guard": ""
        }
      ]
    }

    ---
    # YOUR TURN
    Analyze the user's request. Infer Reachable and DeadlockFree states even if not explicitly named. Follow the recipes strictly. Output ONLY valid JSON matching BehaviourModelDto.
    """;
}