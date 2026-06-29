using System.Collections;
using UnityEngine;

public class SummerPuzzleManager : MonoBehaviour
{
    public static SummerPuzzleManager Instance;

    [Header("퍼즐 구성요소 연결")]
    public StatueController[] statues;          // 석상 4개 (인스펙터에서 연결)
    public SummerSwitchController switchObject;  // 스위치 (리셋 시 기둥 복구용)

    private int _sealedCount = 0;
    private const int TOTAL_STATUES = 4;

    private void Awake()
    {
        Instance = this;
    }

    // 석상 하나 봉인될 때마다 호출
    public void OnStatueSealed()
    {
        _sealedCount++;
        StartCoroutine(ScreenShake());

        if (_sealedCount >= TOTAL_STATUES)
            StartCoroutine(PuzzleClear());
    }

    // 화면 흔들림 (0.5초)
    private IEnumerator ScreenShake()
    {
        Vector3 originPos = Camera.main.transform.position;
        float elapsed = 0f;

        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            float x = originPos.x + Random.Range(-0.1f, 0.1f);
            float y = originPos.y + Random.Range(-0.1f, 0.1f);
            Camera.main.transform.position = new Vector3(x, y, originPos.z);
            yield return null;
        }

        Camera.main.transform.position = originPos;
    }

    // 퍼즐 클리어 연출
    private IEnumerator PuzzleClear()
    {
        Debug.Log("퍼즐 클리어!");
        // TODO: 화이트 페이드아웃 연출 추가
        // TODO: 족자 오브젝트 활성화
        yield return null;
    }

    // 초기화 종 호출 시 전체 리셋
    public void ResetAll()
    {
        _sealedCount = 0;

        // 모든 석상을 시작 위치로 되돌림
        foreach (var statue in statues)
        {
            if (statue != null) statue.ResetToOrigin();
        }

        // 스위치도 초기 상태로 (기둥 다시 활성화)
        if (switchObject != null) switchObject.ResetSwitch();

        Debug.Log("여름방 퍼즐 전체 초기화 완료!");
    }
}
