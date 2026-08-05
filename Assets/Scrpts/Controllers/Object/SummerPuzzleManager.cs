using System.Collections;
using UnityEngine;

public class SummerPuzzleManager : MonoBehaviour
{
    public static SummerPuzzleManager Instance;

    [Header("퍼즐 구성요소 연결")]
    public StatueController[] statues;          // 석상 4개 (인스펙터에서 연결)
    public SummerSwitchController switchObject;  // 스위치 (리셋 시 기둥 복구용)

    [Header("클리어 보상")]
    public GameObject summerScroll;              // 클리어 시 등장할 여름 족자 (맵 중앙)

    private int _sealedCount = 0;
    private const int TOTAL_STATUES = 4;
    private bool _isCleared = false; // 클리어 연출 중복 방지

    private void Awake()
    {
        Instance = this;
    }

#if UNITY_EDITOR
    // 개발용 치트키 (에디터에서만 작동, 실제 빌드엔 포함 안 됨)
    private void Update()
    {
        // F9: 퍼즐 즉시 클리어 (클리어 연출 강제 발동)
        if (Input.GetKeyDown(KeyCode.F9))
        {
            Debug.Log("[치트] F9 - 퍼즐 강제 클리어!");
            if (!_isCleared)
            {
                _isCleared = true;
                StartCoroutine(PuzzleClear());
            }
        }

        // F10: 전체 리셋 (초기화 종과 동일)
        if (Input.GetKeyDown(KeyCode.F10))
        {
            Debug.Log("[치트] F10 - 전체 리셋!");
            ResetAll();
        }
    }
#endif

    // 석상 하나 봉인될 때마다 호출
    public void OnStatueSealed()
    {
        _sealedCount++;
        StartCoroutine(ScreenShake());

        if (_sealedCount >= TOTAL_STATUES && !_isCleared)
        {
            _isCleared = true;
            StartCoroutine(PuzzleClear());
        }
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

        // 마지막 봉인 시 흔들림이 끝날 시간을 살짝 준다
        yield return new WaitForSeconds(0.3f);

        // 1) 0.5초에 걸쳐 화면을 하얗게 덮음
        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeOut(Color.white, 0.5f);

        // 2) 하얀 화면 동안 맵 중앙에 족자 등장
        if (summerScroll != null)
            summerScroll.SetActive(true);

        // 3) 1초에 걸쳐 다시 원래 화면으로 (족자가 드러남)
        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeIn(1f);

        // 아트팀 신규 에셋(펑 효과) - 하얀 화면이 걷히고 족자가 눈에 보이는 시점에 재생
        // (SetActive 시점엔 화면이 하얗게 덮여있어서 안 보이므로 여기서 재생).
        if (summerScroll != null)
        {
            PoofEffect.Spawn(summerScroll.transform.position);
            if (CenterMessageUI.Instance != null)
                CenterMessageUI.Instance.Show("여름 하(夏) 족자가 나타났다.", 2f);
        }
    }

    // 초기화 종 호출 시 전체 리셋
    public void ResetAll()
    {
        _sealedCount = 0;
        _isCleared = false;

        // 족자가 이미 나와있으면 다시 숨김
        if (summerScroll != null) summerScroll.SetActive(false);

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
