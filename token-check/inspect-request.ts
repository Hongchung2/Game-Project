// 작업지시서 #01/#04 연장선 진단: 실제로 Anthropic으로 전송되기 "직전"의 raw 요청 본문을 가로채서
// 그대로 출력한다. SDK의 fetch를 가짜 fetch로 교체해서, 실제 네트워크 전송 전에 강제로 중단시키므로
// 진짜 API 호출은 발생하지 않는다 (비용 0원).

import "dotenv/config";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import Anthropic from "@anthropic-ai/sdk";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const MODEL = "claude-haiku-4-5-20251001";

// 실제로 전송하지 않으므로 진짜 키가 없어도 된다.
const apiKey = process.env.ANTHROPIC_API_KEY ?? "dummy-key-not-actually-sent";

const systemPrompt = fs.readFileSync(path.join(__dirname, "system-prompt.txt"), "utf-8");
const tools = JSON.parse(fs.readFileSync(path.join(__dirname, "tool-schema.json"), "utf-8"));
const sampleProfile = JSON.parse(fs.readFileSync(path.join(__dirname, "sample-profile.json"), "utf-8"));
const userMessage = `[플레이어 정보]\n${JSON.stringify(sampleProfile, null, 2)}\n\n이번 전투의 결정을 내려라.`;

class AbortBeforeSendError extends Error {}

let alreadyLogged = false;

const inspectingFetch = (async (input: any, init: any) => {
  if (!alreadyLogged) {
    alreadyLogged = true;

    console.log("=== 실제 전송 직전 가로챈 요청 ===\n");
    console.log("URL:", input?.toString?.() ?? input);

    const headers: Record<string, string> = { ...(init?.headers ?? {}) };
    if (headers["x-api-key"]) headers["x-api-key"] = "(redacted)";
    if (headers["X-Api-Key"]) headers["X-Api-Key"] = "(redacted)";
    console.log("\nHeaders:", JSON.stringify(headers, null, 2));

    const bodyText = typeof init?.body === "string" ? init.body : String(init?.body ?? "");
    let bodyJson: any = null;
    try {
      bodyJson = JSON.parse(bodyText);
    } catch {
      console.log("\n(body를 JSON으로 파싱하지 못함, raw 출력)");
      console.log(bodyText);
    }

    if (bodyJson) {
      console.log("\nBody:\n" + JSON.stringify(bodyJson, null, 2));

      const systemBlock = Array.isArray(bodyJson.system) ? bodyJson.system[0] : bodyJson.system;
      const hasCacheControl =
        systemBlock && typeof systemBlock === "object" && "cache_control" in systemBlock;

      console.log("\n=== 판정 ===");
      console.log(
        `system 블록에 cache_control 필드: ${hasCacheControl ? "포함됨 ✅" : "누락됨 ❌"}`
      );
      if (hasCacheControl) {
        console.log("cache_control 값:", JSON.stringify(systemBlock.cache_control));
      }
    }
  }

  // 실제 네트워크로 절대 나가지 않도록 여기서 강제 중단 (비용 0원 보장)
  throw new AbortBeforeSendError("진단 전용 — 실제 전송 안 함, API 비용 없음");
}) as unknown as typeof fetch;

const client = new Anthropic({ apiKey, fetch: inspectingFetch, maxRetries: 0 });

async function main() {
  try {
    await client.messages.create({
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
  } catch (err) {
    if (err instanceof AbortBeforeSendError) {
      console.log("\n(의도적으로 여기서 중단함 — 실제 API 호출 없음, 비용 0원)");
      return;
    }
    throw err;
  }
}

main().catch((err) => {
  console.error("진단 스크립트 오류:", err);
  process.exit(1);
});
