using System.Collections;
using UnityEngine;

public class AnswerPedestal : MonoBehaviour
{
    public static AnswerPedestal Instance;

    [Header("단상 정보")]
    public JarController placedJar = null;

    [Header("상승 연출 설정")]
    public float moveDistance = 1.5f; // 위로 이동할 높이
    public float moveSpeed = 2.0f;    // 올라가는 속도
    private Vector3 originPosition;   // 원래 바닥 위치 기억
    private bool isRising = false;    // 현재 상승 중인지 체크

    private Vector3 slotOffset = new Vector3(0f, 0.4f, 0f);

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 게임 시작 시 단상의 원래 위치를 저장해둡니다.
        originPosition = transform.position;
    }

    public bool TryAddJar(JarController jar)
    {
        // 상승 연출 중에는 새로운 항아리를 받지 않습니다.
        if (placedJar != null || isRising) return false;

        placedJar = jar;
        jar.transform.SetParent(transform);
        jar.transform.position = transform.position + slotOffset;

        Debug.Log($"[단상] {jar.jarId}번 항아리가 정답 단상 위에 올라왔습니다. 제출 버튼을 눌러주세요.");
        return true;
    }

    public void RemoveJar(JarController jar)
    {
        if (isRising) return; // 위로 올라가는 중에는 항아리를 뺏지 못하게 막음

        if (placedJar == jar)
        {
            placedJar = null;
            Debug.Log($"[단상] {jar.jarId}번 항아리가 단상에서 내려왔습니다.");
        }
    }

    // [기획 수정 2-2] 제출 버튼을 누르면 호출될 상승 연출 시작 함수
    public void StartRisingPresentation()
    {
        if (placedJar == null)
        {
            Debug.Log("단상 위에 항아리가 없습니다! 항아리를 먼저 올려주세요.");
            return;
        }

        if (!isRising)
        {
            StartCoroutine(RiseAndPopupRoutine());
        }
    }

    // 시간에 따라 부드럽게 위로 이동시키는 코루틴 함수
    private IEnumerator RiseAndPopupRoutine()
    {
        isRising = true;
        Vector3 targetPosition = originPosition + new Vector3(0, moveDistance, 0);

        // 목표 높이에 도달할 때까지 매 프레임 조금씩 이동
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null; // 다음 프레임까지 대기
        }
        transform.position = targetPosition; // 위치 강제 고정

        Debug.Log("[단상] 상승 연출 완료! 정답 UI를 호출합니다.");

        // 연출이 완전히 끝난 직후 UI 팝업창을 띄웁니다!
        if (SpringPuzzleManager.Instance != null)
        {
            SpringPuzzleManager.Instance.ShowAnswerUI();
        }
    }

    // 리셋(초기화)될 때 단상을 다시 바닥 원래 위치로 내리는 함수
    public void ClearPedestal()
    {
        placedJar = null;
        isRising = false;
        transform.position = originPosition;
    }
}