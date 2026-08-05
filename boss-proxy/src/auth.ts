// 게임 클라이언트 요청의 HMAC-SHA256 서명을 검증한다.
// 서명 대상 메시지: `${timestamp}.${rawBody}`

const MAX_CLOCK_SKEW_SECONDS = 5 * 60;

async function hmacSha256Hex(secret: string, message: string): Promise<string> {
  const enc = new TextEncoder();
  const key = await crypto.subtle.importKey(
    "raw",
    enc.encode(secret),
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["sign"]
  );
  const signature = await crypto.subtle.sign("HMAC", key, enc.encode(message));
  return [...new Uint8Array(signature)].map((b) => b.toString(16).padStart(2, "0")).join("");
}

// 타이밍 공격 방지를 위한 상수 시간 비교 (짧은 회로 없이 전체를 훑는다)
function constantTimeEqual(a: string, b: string): boolean {
  if (a.length !== b.length) return false;

  let diff = 0;
  for (let i = 0; i < a.length; i++) {
    diff |= a.charCodeAt(i) ^ b.charCodeAt(i);
  }
  return diff === 0;
}

export async function verifyRequestSignature(
  secret: string,
  timestampHeader: string | null,
  signatureHeader: string | null,
  rawBody: string
): Promise<boolean> {
  if (!timestampHeader || !signatureHeader) return false;

  const timestamp = Number(timestampHeader);
  if (!Number.isFinite(timestamp)) return false;

  const nowSeconds = Math.floor(Date.now() / 1000);
  if (Math.abs(nowSeconds - timestamp) > MAX_CLOCK_SKEW_SECONDS) return false;

  const expected = await hmacSha256Hex(secret, `${timestampHeader}.${rawBody}`);
  return constantTimeEqual(expected, signatureHeader.toLowerCase());
}
