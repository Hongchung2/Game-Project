using UnityEngine;

public class SpringPuzzleManager : MonoBehaviour
{
    public static SpringPuzzleManager Instance;

    [Header("저울판 연결")]
    public ScalePlate leftPlate;
    public ScalePlate rightPlate;

    [Header("UI 및 연출 연결")]
    public GameObject answerUI;
    public GameObject rewardScroll;

    // [추가] 맵 좌측에 배치할 횃불 오브젝트 3개 (인스펙터에서 순서대로 등록)
    public GameObject[] torches = new GameObject[3];

    [Header("퍼즐 정보 (디버그용)")]
    public int oddJarIndex;
    public bool isOddJarHeavier;
    public int weighCount = 0;
    public const int MAX_WEIGH_COUNT = 3;

    public bool isCleared = false;

    private float leftOriginY;
    private float rightOriginY;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        leftOriginY = leftPlate.transform.position.y;
        rightOriginY = rightPlate.transform.position.y;

        SetOddJar();
        weighCount = 0;
        ResetPlatePositions();
        UpdateTorchesVisual(); // 시작할 때 횃불 다 켜기
    }

    private void SetOddJar()
    {
        oddJarIndex = Random.Range(1, 9);
        isOddJarHeavier = Random.value > 0.5f;
        Debug.Log($"[비밀] 이번 스테이지의 범인: {oddJarIndex}번 항아리. (무거운가? {isOddJarHeavier})");
    }

    // [소프트 리셋] 종을 쳤을 때: 정답은 그대로 두고 위치와 횟수, 횃불만 초기화
    public void ResetAll()
    {
        if (isCleared) return;

        // [추가] 리셋 종을 치면 위로 올라갔던 단상도 원래대로 내려오고 초기화됩니다.
        if (AnswerPedestal.Instance != null) AnswerPedestal.Instance.ClearPedestal();

        leftPlate.ClearPlate();
        rightPlate.ClearPlate();

        JarController[] allJars = FindObjectsOfType<JarController>();
        foreach (JarController jar in allJars)
        {
            jar.ResetToOrigin();
        }

        weighCount = 0;
        ResetPlatePositions();
        UpdateTorchesVisual(); // 횃불 다시 다 켜기

        Debug.Log("항아리와 저울 횟수, 횃불이 초기화되었습니다! (범인은 유지)");
    }

    // [하드 리셋] 정답을 틀렸을 때
    private void HardReset()
    {
        if (isCleared) return;

        if (AnswerPedestal.Instance != null) AnswerPedestal.Instance.ClearPedestal();
        leftPlate.ClearPlate();
        rightPlate.ClearPlate();

        JarController[] allJars = FindObjectsOfType<JarController>();
        foreach (JarController jar in allJars)
        {
            jar.ResetToOrigin();
        }

        weighCount = 0;
        ResetPlatePositions();
        SetOddJar();
        UpdateTorchesVisual(); // 횃불 다시 다 켜기

        Debug.Log("💥 하드 리셋 완료! 새로운 범인과 횃불이 재설정되었습니다.");
    }

    // [수정] 무게 비교질 함수 (양쪽 개수가 달라도 작동 가능!)
    public void Weigh()
    {
        if (isCleared) return;

        if (weighCount >= MAX_WEIGH_COUNT)
        {
            Debug.Log("더 이상 저울을 사용할 수 없습니다! 횃불이 모두 꺼졌습니다. 정답을 제출하세요.");
            return;
        }

        int leftCount = leftPlate.jarsOnPlate.Count;
        int rightCount = rightPlate.jarsOnPlate.Count;

        // [기획 수정 1-1] 양쪽 다 0개일 때만 작동을 막고, 한쪽만 올려둔 비대칭 상태는 허용합니다!
        if (leftCount == 0 && rightCount == 0)
        {
            Debug.Log("최소 한쪽 접시에는 항아리를 올려야 작동합니다!");
            return;
        }

        weighCount++;
        UpdateTorchesVisual(); // 무게를 쟀으니 횃불 하나 끄기

        int leftWeight = CalculateWeight(leftPlate);
        int rightWeight = CalculateWeight(rightPlate);

        float moveOffset = 1.0f;

        if (leftWeight > rightWeight)
        {
            Debug.Log($"[결과] 왼쪽이 무겁습니다! (남은 횃불: {MAX_WEIGH_COUNT - weighCount})");
            leftPlate.transform.position = new Vector3(leftPlate.transform.position.x, leftOriginY - moveOffset, leftPlate.transform.position.z);
            rightPlate.transform.position = new Vector3(rightPlate.transform.position.x, rightOriginY + moveOffset, rightPlate.transform.position.z);
        }
        else if (leftWeight < rightWeight)
        {
            Debug.Log($"[결과] 오른쪽이 무겁습니다! (남은 횃불: {MAX_WEIGH_COUNT - weighCount})");
            leftPlate.transform.position = new Vector3(leftPlate.transform.position.x, leftOriginY + moveOffset, leftPlate.transform.position.z);
            rightPlate.transform.position = new Vector3(rightPlate.transform.position.x, rightOriginY - moveOffset, rightPlate.transform.position.z);
        }
        else
        {
            Debug.Log($"[결과] 양쪽이 균형을 이룹니다! (남은 횃불: {MAX_WEIGH_COUNT - weighCount})");
            ResetPlatePositions();
        }
    }

    // [추가 1-3] 계량 횟수에 따라 횃불 오브젝트들을 끄고 켜는 함수
    private void UpdateTorchesVisual()
    {
        for (int i = 0; i < torches.Length; i++)
        {
            if (torches[i] != null)
            {
                // 예: weighCount가 1이면, i가 0인 첫 번째 횃불은 꺼짐(false)이 됩니다.
                torches[i].SetActive(i >= weighCount);
            }
        }
    }

    private int CalculateWeight(ScalePlate plate)
    {
        int weight = 0;
        foreach (JarController jar in plate.jarsOnPlate)
        {
            int jarWeight = 10;
            if (jar.jarId == oddJarIndex)
            {
                jarWeight = isOddJarHeavier ? 11 : 9;
            }
            weight += jarWeight;
        }
        return weight;
    }

    public void ResetPlatePositions()
    {
        leftPlate.transform.position = new Vector3(leftPlate.transform.position.x, leftOriginY, leftPlate.transform.position.z);
        rightPlate.transform.position = new Vector3(rightPlate.transform.position.x, rightOriginY, rightPlate.transform.position.z);
    }

    public void ShowAnswerUI() { if (!isCleared && answerUI != null) answerUI.SetActive(true); }
    public void SubmitAnswer(bool guessIsHeavier)
    {
        if (answerUI != null) answerUI.SetActive(false);
        if (AnswerPedestal.Instance == null || AnswerPedestal.Instance.placedJar == null) return;

        int submittedJarId = AnswerPedestal.Instance.placedJar.jarId;
        string guessText = guessIsHeavier ? "무겁다" : "가볍다";

        if (submittedJarId == oddJarIndex && guessIsHeavier == isOddJarHeavier)
        {
            Debug.Log("🎉 정답입니다! '春' 족자를 획득했습니다!");
            isCleared = true;
            if (rewardScroll != null) rewardScroll.SetActive(true);
        }
        else
        {
            Debug.Log("❌ 오답입니다!");
            HardReset();
        }
    }
}