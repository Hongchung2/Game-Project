# token-check

산수원의 주인(묵운산군) 전투 판단용 시스템 프롬프트 + tool 스키마가
Claude Haiku 4.5의 프롬프트 캐싱 최소 임계값(4,096 tokens)을 넘는지
Anthropic 공식 Token Counting API로 실측하는 스크립트입니다.

Unity 프로젝트와는 무관한 별도 Node.js 스크립트라 `Assets/` 밖(레포 루트)에 둡니다.

## 실행 방법

```bash
cd token-check
npm install
cp .env.example .env
# .env를 열어서 ANTHROPIC_API_KEY=실제키 로 채우기
npm run count
```

## 출력 예시

```
=== 산수원의 주인 프롬프트 토큰 실측 ===
모델: claude-haiku-4-5-20251001
캐싱 최소 임계값: 4,096 tokens

[1] tools only        : XXX tokens
[2] system only       : XXX tokens
[3] tools + system    : XXX tokens  ← 캐시 대상
[4] 전체 (+messages)  : XXX tokens

판정: 통과 / 미달
```

## 참고

- `[1]`, `[2]`, `[3]`은 Token Counting API가 `messages`를 필수로 요구해서
  최소 자리표시 메시지(`"."`) 하나를 포함한 상태로 측정합니다. 핵심 판정 대상인
  `[3] tools + system`이 실제 캐시 프리픽스 크기입니다.
- 시스템 프롬프트 원문은 `system-prompt.txt`에 별도로 두고 코드에 직접 박아넣지
  않습니다 — 공백/줄바꿈이 바뀌면 캐시가 깨질 수 있기 때문입니다.
