export type SystemStatus = 'InvalidLlmName' | 'RequestStarted' | 'BehaviourModelInvalid'

export interface SystemStatusDto {
  type: 'system_status_dto'
  Status: SystemStatus
}

export interface ChunkDto {
  type: 'chunk_dto'
  ChunkText: string
}

export type VariableType = 'Integer' | 'Boolean' | 'Enum'

export interface VariableDefinition {
  VariableName: string
  VariableType: VariableType
  IntegerMin: number | null
  IntegerMax: number | null
  EnumValues: string[] | null
}

export interface VariableAssignment {
  ReferencedVariableName: string
  Value: string
}

export interface Transition {
  Name: string
  Guard: string
  Effects: VariableAssignment[]
}

export type PropertyType = 'Invariant' | 'ForbiddenState' | 'Reachable' | 'DeadlockFree'

export interface Property {
  PropertyType: PropertyType
  Guard: string
}

export interface BehaviourModelDto {
  type: 'behaviour_model'
  IsValidPrompt: boolean
  ReasonIfInvalid: string | null
  Definitions: VariableDefinition[]
  InitialStates: VariableAssignment[]
  Transitions: Transition[]
  Properties: Property[]
}

export type ResponseFrame = SystemStatusDto | ChunkDto | BehaviourModelDto

export function isBehaviourModelDto(v: unknown): v is BehaviourModelDto {
  return typeof v === 'object' && v !== null && (v as { type?: unknown }).type === 'behaviour_model'
}

export function coerceValue(
  def: VariableDefinition | undefined,
  raw: string,
): string | number | boolean {
  if (!def) return raw
  if (def.VariableType === 'Integer') return Number(raw)
  if (def.VariableType === 'Boolean') return raw === 'true'
  return raw
}
