// KV 기반 단순 고정 윈도우(1시간) 카운터. 동시 요청 경합 시 완벽한 원자성은 보장 못하지만
// (KV read-then-write라 레이스 가능성 있음), 뼈대 단계에서는 이 정도로 시작한다.

const WINDOW_SECONDS = 3600;
const SESSION_LIMIT_PER_HOUR = 20;
const IP_LIMIT_PER_HOUR = 60;

export interface RateLimitResult {
  allowed: boolean;
  reason?: "session" | "ip";
}

function currentHourBucket(): number {
  return Math.floor(Date.now() / 1000 / WINDOW_SECONDS);
}

async function checkAndIncrement(kv: KVNamespace, key: string, limit: number): Promise<boolean> {
  const current = await kv.get(key);
  const count = current ? parseInt(current, 10) : 0;

  if (count >= limit) return false;

  // TTL을 윈도우보다 살짝 길게 둬서 버킷 경계 근처 클럭 오차를 흡수
  await kv.put(key, String(count + 1), { expirationTtl: WINDOW_SECONDS + 60 });
  return true;
}

export async function checkRateLimit(
  kv: KVNamespace,
  sessionId: string,
  clientIp: string
): Promise<RateLimitResult> {
  const bucket = currentHourBucket();

  const sessionOk = await checkAndIncrement(kv, `ratelimit:session:${sessionId}:${bucket}`, SESSION_LIMIT_PER_HOUR);
  if (!sessionOk) return { allowed: false, reason: "session" };

  const ipOk = await checkAndIncrement(kv, `ratelimit:ip:${clientIp}:${bucket}`, IP_LIMIT_PER_HOUR);
  if (!ipOk) return { allowed: false, reason: "ip" };

  return { allowed: true };
}
