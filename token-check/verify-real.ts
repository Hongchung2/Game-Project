// 작업지시서 #03 - 작업 2: 진짜 API 키로 재검증.
// 1) 첫 호출(캐시 미스 예상) → cache_creation_input_tokens 확인
// 2) 직후 재호출(같은 프롬프트/같은 PlayerProfile) → cache_read_input_tokens > 0 확인
// 3) 1시간 TTL이 베타 헤더 없이 ephemeral_1h_input_tokens로 찍히는지 확인
// 4) 5~10회 반복해서 지연시간 평균/최대 산출

import "dotenv/config";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import Anthropic from "@anthropic-ai/sdk";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const MODEL = "claude-haiku-4-5-20251001";
const CACHE_THRESHOLD = 4096;
const REPEAT_COUNT = Number(process.env.REPEAT_COUNT ?? 25);
const BUDGET_SECONDS = 5;

const apiKey = process.env.ANTHROPIC_API_KEY;
if (!apiKey) {
  console.error(
    "ANTHROPIC_API_KEY가 설정되어 있지 않습니다. .env.example을 .env로 복사하고 실제 키를 채워주세요."
  );
  process.exit(1);
}

const client = new Anthropic({ apiKey });

const systemPrompt = fs.readFileSync(path.join(__dirname, "system-prompt.txt"), "utf-8");
const tools = JSON.parse(fs.readFileSync(path.join(__dirname, "tool-schema.json"), "utf-8"));
const sampleProfile = JSON.parse(
  fs.readFileSync(path.join(__dirname, "sample-profile.json"), "utf-8")
);

const userMessage = `[플레이어 정보]\n${JSON.stringify(sampleProfile, null, 2)}\n\n이번 전투의 결정을 내려라.`;

interface CallStats {
  latencyMs: number;
  inputTokens: number;
  cacheCreationInputTokens: number;
  cacheReadInputTokens: number;
  ephemeral1h: number;
  ephemeral5m: number;
}

async function callOnce(): Promise<CallStats> {
  const start = Date.now();
  const response = await client.messages.create({
    model: MODEL,
    max_tokens: 1024,
    temperature: 0.7,
    system: [
      {
        type: "text",
        text: systemPrompt,
        cache_control: { type: "ephemeral", ttl: "1h" },
      },
    ],
    tools,
    tool_choice: { type: "tool", name: "generate_boss_decision" },
    messages: [{ role: "user", content: userMessage }],
  });
  const latencyMs = Date.now() - start;

  return {
    latencyMs,
    inputTokens: response.usage.input_tokens,
    cacheCreationInputTokens: response.usage.cache_creation_input_tokens ?? 0,
    cacheReadInputTokens: response.usage.cache_read_input_tokens ?? 0,
    ephemeral1h: response.usage.cache_creation?.ephemeral_1h_input_tokens ?? 0,
    ephemeral5m: response.usage.cache_creation?.ephemeral_5m_input_tokens ?? 0,
  };
}

// 참고용: 작업지시서 #01의 tools+system 토큰 실측치도 같이 찍어준다 (count.ts와 동일 로직)
async function countCachePrefixTokens(): Promise<number> {
  const res = await client.messages.countTokens({
    model: MODEL,
    messages: [{ role: "user", content: "." }],
    system: systemPrompt,
    tools,
  });
  return res.input_tokens;
}

async function main() {
  const cachePrefixTokens = await countCachePrefixTokens();

  console.log(`[1] 첫 호출 (캐시 미스 예상)`);
  const first = await callOnce();
  console.log(`  latency: ${first.latencyMs}ms`);
  console.log(`  cache_creation_input_tokens: ${first.cacheCreationInputTokens}`);
  console.log(`  cache_read_input_tokens: ${first.cacheReadInputTokens}`);
  console.log(`  ephemeral_1h_input_tokens: ${first.ephemeral1h}`);
  console.log(`  ephemeral_5m_input_tokens: ${first.ephemeral5m}`);
  console.log("");

  console.log(`[2] 직후 재호출 (같은 프롬프트/프로필, 캐시 히트 예상)`);
  const second = await callOnce();
  console.log(`  latency: ${second.latencyMs}ms`);
  console.log(`  cache_creation_input_tokens: ${second.cacheCreationInputTokens}`);
  console.log(`  cache_read_input_tokens: ${second.cacheReadInputTokens}`);
  console.log(`  ephemeral_1h_input_tokens: ${second.ephemeral1h}`);
  console.log("");

  console.log(`[3~${REPEAT_COUNT}] 추가 반복 호출 (지연시간 표본 확보)`);
  const allStats = [first, second];
  for (let i = 2; i < REPEAT_COUNT; i++) {
    const s = await callOnce();
    allStats.push(s);
    console.log(`  ${i + 1}번째: ${s.latencyMs}ms, cache_read=${s.cacheReadInputTokens}`);
  }

  const latencies = allStats.map((s) => s.latencyMs);
  const sortedLatencies = [...latencies].sort((a, b) => a - b);
  const avgLatency = latencies.reduce((a, b) => a + b, 0) / latencies.length;
  const maxLatency = Math.max(...latencies);
  const medianLatency =
    sortedLatencies.length % 2 === 1
      ? sortedLatencies[(sortedLatencies.length - 1) / 2]
      : (sortedLatencies[sortedLatencies.length / 2 - 1] + sortedLatencies[sortedLatencies.length / 2]) / 2;

  const overBudgetCount = latencies.filter((ms) => ms > BUDGET_SECONDS * 1000).length;
  const overBudgetPct = ((overBudgetCount / latencies.length) * 100).toFixed(0);

  // 캐시 히트율: 첫 호출은 미스가 정상이므로 2번째 호출부터 집계
  const cacheEligible = allStats.slice(1);
  const cacheHits = cacheEligible.filter((s) => s.cacheReadInputTokens > 0).length;

  const ttlOk = allStats.some((s) => s.ephemeral1h > 0);

  console.log("\n=== 재검증 결과 ===");
  console.log(
    `[토큰] tools+system: ${cachePrefixTokens} tokens (4,096 대비 ${cachePrefixTokens >= CACHE_THRESHOLD ? "통과" : "미달"})`
  );
  console.log("");
  console.log(`[지연시간 — ${allStats.length}회]`);
  console.log(`평균: ${avgLatency.toFixed(0)}ms / 최대: ${maxLatency}ms / 중앙값: ${medianLatency.toFixed(0)}ms`);
  console.log(`5초 초과 횟수: ${overBudgetCount}회 / ${allStats.length}회 (${overBudgetPct}%)`);
  console.log("");
  console.log("[캐시]");
  console.log(`캐시 히트율: ${cacheHits}회 / ${cacheEligible.length}회 (첫 호출 제외)`);
  console.log(`1시간 TTL 작동 여부: ${ttlOk ? "예" : "아니오"}`);
}

main().catch((err) => {
  console.error("실키 검증 중 오류 발생:", err);
  process.exit(1);
});
