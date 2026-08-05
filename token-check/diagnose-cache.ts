import "dotenv/config";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import Anthropic from "@anthropic-ai/sdk";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const MODEL = "claude-haiku-4-5-20251001";

const apiKey = process.env.ANTHROPIC_API_KEY!;
const client = new Anthropic({ apiKey });

const systemPrompt = fs.readFileSync(path.join(__dirname, "system-prompt.txt"), "utf-8");
const tools = JSON.parse(fs.readFileSync(path.join(__dirname, "tool-schema.json"), "utf-8"));
const sampleProfile = JSON.parse(fs.readFileSync(path.join(__dirname, "sample-profile.json"), "utf-8"));
const userMessage = `[플레이어 정보]\n${JSON.stringify(sampleProfile, null, 2)}\n\n이번 전투의 결정을 내려라.`;

async function callWith(cacheControl: any, label: string) {
  const system: any[] = [{ type: "text", text: systemPrompt }];
  if (cacheControl) system[0].cache_control = cacheControl;

  const res = await client.messages.create({
    model: MODEL,
    max_tokens: 1024,
    temperature: 0.7,
    system,
    tools,
    tool_choice: { type: "tool", name: "generate_boss_decision" },
    messages: [{ role: "user", content: userMessage }],
  });

  console.log(`--- ${label} ---`);
  console.log(JSON.stringify(res.usage, null, 2));
}

async function main() {
  console.log("=== [A] cache_control 없음 (베이스라인) ===");
  await callWith(undefined, "A-1 no cache_control");
  await callWith(undefined, "A-2 no cache_control (재호출)");

  console.log("\n=== [B] cache_control ephemeral, ttl 없음 (기본 5분) ===");
  await callWith({ type: "ephemeral" }, "B-1 5m default");
  await callWith({ type: "ephemeral" }, "B-2 5m default (재호출)");

  console.log("\n=== [C] cache_control ephemeral, ttl: 1h ===");
  await callWith({ type: "ephemeral", ttl: "1h" }, "C-1 1h ttl");
  await callWith({ type: "ephemeral", ttl: "1h" }, "C-2 1h ttl (재호출)");
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
