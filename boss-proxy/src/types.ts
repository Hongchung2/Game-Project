export interface Env {
  ANTHROPIC_API_KEY: string;
  SHARED_SECRET: string;
  KV: KVNamespace;
}

export interface BossDecisionRequestBody {
  session_id: string;
  player_profile: unknown;
  attempt: number;
}

export type FallbackReason = "timeout" | "schema_violation" | "api_error" | "kill_switch";

// tool-schema.json의 generate_boss_decision.input_schema를 그대로 옮긴 타입
export interface BossDecision {
  mark_weight: { mountain: number; cloud: number };
  mountain_target: "habitual_position" | "current_position";
  phase2_pattern_weight: { punch: number; slam: number };
  phase2_recovery_exposed: boolean;
  boss_line_p1: string;
  boss_line_transition: string;
  confidence: "low" | "mid" | "high";
}

export interface BossDecisionResponseBody {
  ok: boolean;
  decision: unknown | null;
  meta: {
    cache_read_tokens?: number;
    cache_creation_tokens?: number;
    cache_creation_ephemeral_1h_tokens?: number;
    cache_creation_ephemeral_5m_tokens?: number;
    latency_ms: number;
    fallback: boolean;
    reason?: FallbackReason;
  };
}
