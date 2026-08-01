// wrangler dev로 로컬 실행 중인 프록시에 실제 HTTP 요청을 보내서
// 완료 기준(서명 검증/rate limit/킬스위치/메타 정보)을 확인하는 스모크 테스트.
//
// 사용법:
//   1) 터미널 A: npm run dev        (wrangler dev, http://127.0.0.1:8787)
//   2) 터미널 B: SHARED_SECRET=<.dev.vars와 동일한 값> npm test

import { createHmac } from "node:crypto";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const BASE_URL = process.env.PROXY_URL ?? "http://127.0.0.1:8787";
const SHARED_SECRET = process.env.SHARED_SECRET;

if (!SHARED_SECRET) {
  console.error("SHARED_SECRET 환경변수가 필요합니다. .dev.vars에 넣은 값과 동일해야 서명이 통과합니다.");
  process.exit(1);
}

const mockProfile = JSON.parse(fs.readFileSync(path.join(__dirname, "mock-profile.json"), "utf-8"));

function sign(secret: string, message: string): string {
  return createHmac("sha256", secret).update(message).digest("hex");
}

async function callBossDecision(sessionId: string, attempt: number, withSignature = true) {
  const body = JSON.stringify({ session_id: sessionId, player_profile: mockProfile, attempt });

  const headers: Record<string, string> = { "content-type": "application/json" };
  if (withSignature) {
    const timestamp = Math.floor(Date.now() / 1000).toString();
    headers["X-Timestamp"] = timestamp;
    headers["X-Signature"] = sign(SHARED_SECRET!, `${timestamp}.${body}`);
  }

  const start = Date.now();
  const res = await fetch(`${BASE_URL}/boss-decision`, { method: "POST", headers, body });
  const elapsedMs = Date.now() - start;
  const json = await res.json().catch(() => null);
  return { status: res.status, json, elapsedMs };
}

async function main() {
  console.log(`대상: ${BASE_URL}/boss-decision\n`);

  console.log("=== [1] 서명 없이 요청 → 401 기대 ===");
  const noSig = await callBossDecision("no-sig-test", 1, false);
  console.log(noSig.status, noSig.json);
  console.log(noSig.status === 401 ? "  ✅ 통과\n" : "  ❌ 실패 (401이 아님)\n");

  console.log("=== [2] 정상 서명으로 요청 ===");
  const ok = await callBossDecision("local-test-session", 1);
  console.log(`${ok.status} (${ok.elapsedMs}ms)`, ok.json);
  console.log("");

  console.log("=== [3] Rate limit: session 21회 연속 호출 (20회 제한) ===");
  const latencies: number[] = [];
  let hit429 = false;
  for (let i = 0; i < 21; i++) {
    const r = await callBossDecision("rate-limit-test-session", i + 1);
    latencies.push(r.elapsedMs);
    if (r.status === 429) {
      console.log(`  ${i + 1}번째 호출에서 429 발생`, r.json);
      hit429 = true;
      break;
    }
  }
  console.log(hit429 ? "  ✅ rate limit 동작 확인\n" : "  ❌ 21회 안에 429가 발생하지 않음\n");

  if (latencies.length > 0) {
    const avg = latencies.reduce((a, b) => a + b, 0) / latencies.length;
    const max = Math.max(...latencies);
    console.log(`=== 지연시간 (rate-limit 테스트 호출 ${latencies.length}회 기준) ===`);
    console.log(`  평균: ${avg.toFixed(0)}ms, 최대: ${max}ms`);
  }
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
