import type { BossDecision } from "./types";

// tool-schema.json의 minimum/maximum과 동일한 범위. 스키마가 API 레벨에서 강제하긴 하지만,
// LLM이 지시를 어길 가능성에 대비해 여기서도 이중으로 clamp한다 (작업지시서 #03 - 작업 4).
const WEIGHT_MIN = 0.3;
const WEIGHT_MAX = 0.7;
const LINE_MAX_LENGTH = 40;

const MOUNTAIN_TARGETS = ["habitual_position", "current_position"] as const;
const CONFIDENCE_LEVELS = ["low", "mid", "high"] as const;

const ALLOWED_TOP_LEVEL_KEYS = [
  "mark_weight",
  "mountain_target",
  "phase2_pattern_weight",
  "phase2_recovery_exposed",
  "boss_line_p1",
  "boss_line_transition",
  "confidence",
] as const;

function isFiniteNumber(value: unknown): value is number {
  return typeof value === "number" && Number.isFinite(value);
}

function isNonEmptyStringWithinLength(value: unknown, maxLength: number): value is string {
  return typeof value === "string" && value.length > 0 && value.length <= maxLength;
}

function hasExactKeys(obj: Record<string, unknown>, allowedKeys: readonly string[]): boolean {
  const keys = Object.keys(obj);
  if (keys.length !== allowedKeys.length) return false;
  return allowedKeys.every((k) => k in obj);
}

// tool-schema.json(generate_boss_decision)을 그대로 코드로 옮긴 필드 단위 검증.
// 여기서는 "구조가 맞는가"만 확인한다 (타입/enum/길이/additionalProperties:false 대응).
// 가중치의 0.3~0.7 범위는 검증 실패 사유로 삼지 않고, 통과 후 clampDecisionWeights()로 강제 보정한다
// — API 스키마가 이미 막아주는 걸 믿되, 혹시 새어나온 값을 버리지 않고 살려서 쓰기 위함.
export function isValidDecision(obj: unknown): obj is BossDecision {
  if (obj === null || typeof obj !== "object") return false;
  const d = obj as Record<string, unknown>;

  if (!hasExactKeys(d, ALLOWED_TOP_LEVEL_KEYS)) return false;

  const markWeight = d.mark_weight;
  if (typeof markWeight !== "object" || markWeight === null) return false;
  if (!hasExactKeys(markWeight as Record<string, unknown>, ["mountain", "cloud"])) return false;
  if (!isFiniteNumber((markWeight as Record<string, unknown>).mountain)) return false;
  if (!isFiniteNumber((markWeight as Record<string, unknown>).cloud)) return false;

  if (!MOUNTAIN_TARGETS.includes(d.mountain_target as (typeof MOUNTAIN_TARGETS)[number])) return false;

  const phase2 = d.phase2_pattern_weight;
  if (typeof phase2 !== "object" || phase2 === null) return false;
  if (!hasExactKeys(phase2 as Record<string, unknown>, ["punch", "slam"])) return false;
  if (!isFiniteNumber((phase2 as Record<string, unknown>).punch)) return false;
  if (!isFiniteNumber((phase2 as Record<string, unknown>).slam)) return false;

  if (typeof d.phase2_recovery_exposed !== "boolean") return false;

  if (!isNonEmptyStringWithinLength(d.boss_line_p1, LINE_MAX_LENGTH)) return false;
  if (!isNonEmptyStringWithinLength(d.boss_line_transition, LINE_MAX_LENGTH)) return false;

  if (!CONFIDENCE_LEVELS.includes(d.confidence as (typeof CONFIDENCE_LEVELS)[number])) return false;

  return true;
}

function clamp(value: number, min = WEIGHT_MIN, max = WEIGHT_MAX): number {
  return Math.min(max, Math.max(min, value));
}

// isValidDecision을 통과한 뒤에만 호출할 것 — 형태 자체가 틀리면 여기서 손댈 수 없다.
export function clampDecisionWeights(decision: BossDecision): BossDecision {
  return {
    ...decision,
    mark_weight: {
      mountain: clamp(decision.mark_weight.mountain),
      cloud: clamp(decision.mark_weight.cloud),
    },
    phase2_pattern_weight: {
      punch: clamp(decision.phase2_pattern_weight.punch),
      slam: clamp(decision.phase2_pattern_weight.slam),
    },
  };
}
