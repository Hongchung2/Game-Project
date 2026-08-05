#if UNITY_EDITOR
using System.Net.Sockets;
using UnityEditor;
using UnityEngine;

// 로컬 wrangler dev(127.0.0.1:8787)가 켜져 있는지 확인하는 도구.
// HTTP 요청/서명까지는 안 가고 TCP 접속만 시도 - "서버 프로세스가 떠있는가"만 빠르게 확인하면
// 충분함(플레이테스트 중 프록시가 조용히 꺼져있던 걸 몰랐던 사고가 있었음).
public static class BossProxyHealthCheck
{
    private const string HOST = "127.0.0.1";
    private const int PORT = 8787;
    private const int TIMEOUT_MS = 1000;

    [MenuItem("Tools/Boss/Check Proxy Status")]
    public static void CheckStatus()
    {
        if (IsProxyReachable())
            Debug.Log("[BossProxyHealthCheck] OK - 로컬 프록시(127.0.0.1:8787) 켜져있음");
        else
            Debug.LogWarning("[BossProxyHealthCheck] 실패 - 로컬 프록시(127.0.0.1:8787) 응답 없음. wrangler dev가 꺼져있을 수 있음");
    }

    public static bool IsProxyReachable()
    {
        try
        {
            using (var client = new TcpClient())
            {
                var result = client.BeginConnect(HOST, PORT, null, null);
                bool connected = result.AsyncWaitHandle.WaitOne(TIMEOUT_MS);
                if (!connected) return false;
                client.EndConnect(result);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }
}
#endif
