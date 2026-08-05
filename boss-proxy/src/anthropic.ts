import Anthropic from "@anthropic-ai/sdk";
// @ts-ignore - wrangler.toml의 Text 모듈 규칙으로 .txt를 문자열로 import
import systemPromptText from "./prompt/system-prompt.txt";
import toolSchema from "./prompt/tool-schema.json";
import type { FallbackReason, BossDecision } from "./types";
import { isValidDecision, clampDecisionWeights } from "./validation";

const MODEL = "claude-haiku-4-5-20251001"; // 별칭(claude-haiku-4-5) 사용 금지 — 날짜 버전 고정
const PRIMARY_TIMEOUT_MS = 5000; // 실측 결과 콜드 캐시 호출이 2.6~3.5초대라 3.5초 예산은 정상 응답도 타임아웃시켰음(플레이테스트 중 발견) — 여유를 둠
const RETRY_TIMEOUT_MS = 1500;

export interface BossDecisionResult {
  decision: BossDecision | null;
  fallback: boolean;
  reason?: FallbackReason;
  cacheReadTokens: number;
  cacheCreationTokens: number;
  // 1시간 TTL이 실제로 적용됐는지 확인용 세부 내역 (작업지시서 #03 - 작업 2.3)
  cacheCreationEphemeral1h: number;
  cacheCreationEphemeral5m: number;
}

function buildUserMessage(playerProfile: unknown): string {
  return `[플레이어 정보]\n${JSON.stringify(playerProfile, null, 2)}\n\n이번 전투의 결정을 내려라.`;
}

function extractToolUse(response: Anthropic.Message): unknown | null {
  const block = response.content.find(
    (b): b is Anthropic.ToolUseBlock => b.type === "tool_use" && b.name === "generate_boss_decision"
  );
  return block ? block.input : null;
}

function isRetryableError(err: unknown): boolean {
  if (err instanceof Anthropic.APIConnectionError) return true;
  if (err instanceof Anthropic.APIError && typeof err.status === "number" && err.status >= 500) return true;
  return false;
}

async function callOnce(
  client: Anthropic,
  playerProfile: unknown,
  timeoutMs: number
): Promise<Anthropic.Message> {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);

  try {
    return await client.messages.create(
      {
        model: MODEL,
        max_tokens: 1024,
        temperature: 0.7,
        system: [
          {
            type: "text",
            text: systemPromptText,
            cache_control: { type: "ephemeral", ttl: "1h" },
          },
        ],
        tools: toolSchema as Anthropic.Tool[],
        tool_choice: { type: "tool", name: "generate_boss_decision" },
        messages: [{ role: "user", content: buildUserMessage(playerProfile) }],
      },
      { signal: controller.signal }
    );
  } finally {
    clearTimeout(timer);
  }
}

function resultFromResponse(response: Anthropic.Message): BossDecisionResult {
  const rawDecision = extractToolUse(response);

  if (!isValidDecision(rawDecision)) {
    return fallback("schema_violation");
  }

  return {
    decision: clampDecisionWeights(rawDecision),
    fallback: false,
    cacheReadTokens: response.usage.cache_read_input_tokens ?? 0,
    cacheCreationTokens: response.usage.cache_creation_input_tokens ?? 0,
    cacheCreationEphemeral1h: response.usage.cache_creation?.ephemeral_1h_input_tokens ?? 0,
    cacheCreationEphemeral5m: response.usage.cache_creation?.ephemeral_5m_input_tokens ?? 0,
  };
}

function fallback(reason: FallbackReason): BossDecisionResult {
  return {
    decision: null,
    fallback: true,
    reason,
    cacheReadTokens: 0,
    cacheCreationTokens: 0,
    cacheCreationEphemeral1h: 0,
    cacheCreationEphemeral5m: 0,
  };
}

export async function requestBossDecision(
  apiKey: string,
  playerProfile: unknown
): Promise<BossDecisionResult> {
  const client = new Anthropic({ apiKey });

  try {
    const response = await callOnce(client, playerProfile, PRIMARY_TIMEOUT_MS);
    return resultFromResponse(response);
  } catch (err) {
    // AbortController로 끊은 타임아웃이 SDK를 거치며 "AbortError"가 아니라 일반 Error(메시지에
    // aborted 포함)로 올라오는 경우가 실측에서 확인됨 - 그 경우 그냥 지나쳐서 "timeout"이 아니라
    // "api_error"로 잘못 기록되고 있었음(플레이테스트 중 발견, 원인 진단용 로그로 확인).
    if (err instanceof Error && (err.name === "AbortError" || /abort/i.test(err.message))) {
      // 타임아웃은 재시도하지 않음 (예산 초과 위험) — 즉시 fallback
      return fallback("timeout");
    }

    if (!isRetryableError(err)) {
      return fallback("api_error");
    }

    // 5xx/네트워크 오류에 한해 1회만 재시도 (최대 +1.5초 예산)
    try {
      const response = await callOnce(client, playerProfile, RETRY_TIMEOUT_MS);
      return resultFromResponse(response);
    } catch {
      return fallback("api_error");
    }
  }
}
