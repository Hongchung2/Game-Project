# boss-proxy

산수원의 주인(묵운산군) 전투 판단 요청을 게임 클라이언트 대신 대행하는 Cloudflare Workers 프록시.
게임이 Anthropic API 키를 직접 들고 있지 않도록, 이 프록시에만 키를 둔다.

**뼈대 단계**: 시스템 프롬프트/tool 스키마는 작업지시서 #01 결과 확정 전이라 `src/prompt/`에
플레이스홀더로만 들어있다. 구조(인증, rate limit, 킬 스위치, 타임아웃/재시도, 로깅)가 이번 작업의 핵심.

## 로컬 실행

```bash
npm install
cp .dev.vars.example .dev.vars
# .dev.vars를 열어서 ANTHROPIC_API_KEY, SHARED_SECRET을 채우기
npm run dev
```

`wrangler dev`가 뜨면 기본적으로 `http://127.0.0.1:8787`에서 로컬 KV(인메모리)로 동작한다.
(실제 Cloudflare KV/배포가 필요하면 `wrangler kv namespace create boss-proxy-kv`로 만든 뒤
`wrangler.toml`의 `id`/`preview_id`를 채우고 `wrangler secret put ANTHROPIC_API_KEY` 등으로
프로덕션 시크릿을 등록한다.)

## 스모크 테스트

`npm run dev`를 켜둔 상태에서 다른 터미널에서:

```bash
SHARED_SECRET=<.dev.vars와 동일한 값> npm test
```

`test/local-test.ts`가 아래를 확인한다:
1. 서명 없이 요청 → 401
2. 정상 서명으로 요청 → Anthropic 호출까지 도달 (mock 프로필 사용)
3. 같은 session_id로 21회 연속 호출 → 20회째 이후 429

## 킬 스위치 토글

```bash
# 로컬(wrangler dev)에서 끄기
wrangler kv key put --binding=KV llm_enabled false --local

# 다시 켜기
wrangler kv key put --binding=KV llm_enabled true --local
```

배포 후에는 `--local` 플래그를 빼면 실제 Cloudflare KV에 적용된다 (재배포 없이 즉시 반영).

## 엔드포인트

```
POST /boss-decision

요청 헤더:
  X-Timestamp: <유닉스 타임(초)>
  X-Signature: HMAC-SHA256(SHARED_SECRET, `${timestamp}.${rawBody}`)

요청 바디:
{ "session_id": string, "player_profile": object, "attempt": number }

응답:
{
  "ok": true,
  "decision": {...} | null,
  "meta": {
    "cache_read_tokens": number,
    "cache_creation_tokens": number,
    "latency_ms": number,
    "fallback": boolean,
    "reason"?: "timeout" | "schema_violation" | "api_error" | "kill_switch"
  }
}
```

## 남은 TODO (작업지시서 #01/설계 확정 후)

- `src/prompt/system-prompt.txt`, `src/prompt/tool-schema.json`을 실제 확정본으로 교체
- `anthropic.ts`의 `isValidDecision`을 필드 단위 검증(가중치 범위, 문자열 길이 등)으로 강화
- 로깅을 `console.log` 대신 Cloudflare Analytics Engine 등 영속 저장소로 연결
