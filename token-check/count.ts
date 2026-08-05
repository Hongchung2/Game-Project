import "dotenv/config";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import Anthropic from "@anthropic-ai/sdk";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const MODEL = "claude-haiku-4-5-20251001";
const CACHE_THRESHOLD = 4096;

const apiKey = process.env.ANTHROPIC_API_KEY;
if (!apiKey) {
  console.error(
    "ANTHROPIC_API_KEY가 설정되어 있지 않습니다. .env.example을 .env로 복사하고 키를 채워주세요."
  );
  process.exit(1);
}

const client = new Anthropic({ apiKey });

const systemPrompt = fs.readFileSync(path.join(__dirname, "system-prompt.txt"), "utf-8");
const tools = JSON.parse(fs.readFileSync(path.join(__dirname, "tool-schema.json"), "utf-8"));
const sampleProfile = JSON.parse(
  fs.readFileSync(path.join(__dirname, "sample-profile.json"), "utf-8")
);

// key 순서 고정을 위해 sample-profile.json에 이미 기록된 순서를 그대로 직렬화한다.
const sampleUserMessage =
  `[플레이어 정보]\n${JSON.stringify(sampleProfile, null, 2)}\n\n이번 전투의 결정을 내려라.`;

// countTokens 엔드포인트도 messages.create처럼 messages가 비어있으면 안 되므로,
// [1]~[3]은 최소 자리표시 메시지 하나만 넣어서 해당 구성요소의 크기만 격리 측정한다.
const PLACEHOLDER_MESSAGE = [{ role: "user" as const, content: "." }];

async function countTokens(params: Anthropic.Messages.MessageCountTokensParams): Promise<number> {
  const res = await client.messages.countTokens(params);
  return res.input_tokens;
}

async function main() {
  const toolsOnly = await countTokens({
    model: MODEL,
    messages: PLACEHOLDER_MESSAGE,
    tools,
  });

  const systemOnly = await countTokens({
    model: MODEL,
    messages: PLACEHOLDER_MESSAGE,
    system: systemPrompt,
  });

  const toolsPlusSystem = await countTokens({
    model: MODEL,
    messages: PLACEHOLDER_MESSAGE,
    system: systemPrompt,
    tools,
  });

  const full = await countTokens({
    model: MODEL,
    messages: [{ role: "user", content: sampleUserMessage }],
    system: systemPrompt,
    tools,
  });

  const pass = toolsPlusSystem >= CACHE_THRESHOLD;

  console.log("=== 산수원의 주인 프롬프트 토큰 실측 ===");
  console.log(`모델: ${MODEL}`);
  console.log(`캐싱 최소 임계값: ${CACHE_THRESHOLD.toLocaleString()} tokens`);
  console.log("");
  console.log(`[1] tools only        : ${toolsOnly} tokens`);
  console.log(`[2] system only       : ${systemOnly} tokens`);
  console.log(`[3] tools + system    : ${toolsPlusSystem} tokens  ← 캐시 대상`);
  console.log(`[4] 전체 (+messages)  : ${full} tokens`);
  console.log("");

  if (pass) {
    console.log("판정: 통과");
    console.log(`  - 임계값 대비 +${toolsPlusSystem - CACHE_THRESHOLD} tokens 여유`);
  } else {
    console.log("판정: 미달");
    console.log(`  - 임계값까지 ${CACHE_THRESHOLD - toolsPlusSystem} tokens 부족`);
  }
}

main().catch((err) => {
  console.error("토큰 측정 중 오류 발생:", err);
  process.exit(1);
});
