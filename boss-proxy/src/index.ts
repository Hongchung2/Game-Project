import { verifyRequestSignature } from "./auth";
import { checkRateLimit } from "./ratelimit";
import { isLlmEnabled } from "./killswitch";
import { requestBossDecision } from "./anthropic";
import type { Env, BossDecisionRequestBody, BossDecisionResponseBody } from "./types";

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "content-type": "application/json" },
  });
}

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);

    if (request.method !== "POST" || url.pathname !== "/boss-decision") {
      return jsonResponse({ ok: false, reason: "not_found" }, 404);
    }

    const start = Date.now();
    const rawBody = await request.text();

    const validSignature = await verifyRequestSignature(
      env.SHARED_SECRET,
      request.headers.get("X-Timestamp"),
      request.headers.get("X-Signature"),
      rawBody
    );
    if (!validSignature) {
      return jsonResponse({ ok: false, reason: "unauthorized" }, 401);
    }

    let payload: BossDecisionRequestBody;
    try {
      payload = JSON.parse(rawBody);
    } catch {
      return jsonResponse({ ok: false, reason: "invalid_json" }, 400);
    }

    if (!payload.session_id || typeof payload.session_id !== "string") {
      return jsonResponse({ ok: false, reason: "missing_session_id" }, 400);
    }

    // 개인정보 보호: 원본 IP는 rate limit 버킷 키로만 쓰고 로그에는 남기지 않는다.
    const clientIp = request.headers.get("CF-Connecting-IP") ?? "unknown";
    const rate = await checkRateLimit(env.KV, payload.session_id, clientIp);
    if (!rate.allowed) {
      return jsonResponse({ ok: false, reason: "rate_limited" }, 429);
    }

    const llmEnabled = await isLlmEnabled(env.KV);
    if (!llmEnabled) {
      const body: BossDecisionResponseBody = {
        ok: true,
        decision: null,
        meta: {
          fallback: true,
          reason: "kill_switch",
          latency_ms: Date.now() - start,
          cache_read_tokens: 0,
          cache_creation_tokens: 0,
        },
      };
      return jsonResponse(body);
    }

    const result = await requestBossDecision(env.ANTHROPIC_API_KEY, payload.player_profile);
    const latencyMs = Date.now() - start;

    // TODO(로깅): Cloudflare Analytics Engine 등으로 영속 저장.
    // session_id/latency/cache 토큰/fallback 여부만 남기고 실명·SteamID·원본 IP는 남기지 않는다.
    console.log(
      JSON.stringify({
        session_id: payload.session_id,
        ts: start,
        latency_ms: latencyMs,
        cache_read_tokens: result.cacheReadTokens,
        cache_creation_tokens: result.cacheCreationTokens,
        cache_creation_ephemeral_1h_tokens: result.cacheCreationEphemeral1h,
        cache_creation_ephemeral_5m_tokens: result.cacheCreationEphemeral5m,
        fallback: result.fallback,
        fallback_reason: result.reason ?? null,
      })
    );

    const body: BossDecisionResponseBody = {
      ok: true,
      decision: result.decision,
      meta: {
        cache_read_tokens: result.cacheReadTokens,
        cache_creation_tokens: result.cacheCreationTokens,
        cache_creation_ephemeral_1h_tokens: result.cacheCreationEphemeral1h,
        cache_creation_ephemeral_5m_tokens: result.cacheCreationEphemeral5m,
        latency_ms: latencyMs,
        fallback: result.fallback,
        ...(result.reason ? { reason: result.reason } : {}),
      },
    };

    return jsonResponse(body);
  },
};
