const KILL_SWITCH_KEY = "llm_enabled";

// KV에 플래그가 아직 없으면(최초 배포 등) 켜진 것으로 간주한다 —
// 배포 직후 의도치 않게 전부 fallback으로 빠지는 것을 방지하기 위함.
export async function isLlmEnabled(kv: KVNamespace): Promise<boolean> {
  const value = await kv.get(KILL_SWITCH_KEY);
  if (value === null) return true;
  return value === "true";
}

// CLI(`wrangler kv key put`)나 대시보드에서 이 키를 "false"로 바꾸면
// 배포 없이 즉시 LLM 호출이 꺼진다.
export async function setLlmEnabled(kv: KVNamespace, enabled: boolean): Promise<void> {
  await kv.put(KILL_SWITCH_KEY, enabled ? "true" : "false");
}
