using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 정화수 양동이 퍼즐 (14L / 9L / 5L) 로직.
// 화면(양동이 그림/버튼/텍스트)은 유니티 에디터에서 직접 만들어서 아래 필드에 연결한다.
// 조작: 양동이를 드래그해서 다른 양동이 위에 드롭하면 붓는다(DraggableBucket 참고).
// 규칙: 한 양동이가 비거나 다른 양동이가 가득 찰 때까지 붓는다(중간 멈춤 불가).
// 목표: 14L 양동이와 9L 양동이에 각각 정확히 7L씩 나누기.
public class WaterPuzzleUI : MonoBehaviour
{
    public static WaterPuzzleUI Instance;

    [Header("패널 (전체 퍼즐 창)")]
    public GameObject panel;

    [Header("양동이 3개 - 순서 고정: 0=14L, 1=9L, 2=5L")]
    public Image[] bucketFills = new Image[3];       // Image Type을 Filled로 설정해서 연결 (물 차오르는 표시)
    public TMP_Text[] amountTexts = new TMP_Text[3]; // "5L / 14L" 같은 표시
    public Button[] bucketButtons = new Button[3];   // 각 양동이 오브젝트 (DraggableBucket도 여기 붙어있음)
    public Image[] bucketBackgrounds;                // 붓는 중인 양동이 강조용 (선택 사항, 비워도 됨)

    [Header("붓기 연출")]
    public float tiltDuration = 0.25f;   // 기울이는 시간
    public float pourDuration = 0.5f;    // 물이 옮겨가는 시간
    public float tiltAngle = 50f;        // 기울이는 각도

    [Header("강조 색상 (붓는 중인 양동이)")]
    public Color normalColor = Color.white;
    public Color pouringColor = new Color(0.55f, 0.75f, 1f);

    [Header("힌트")]
    public GameObject hintPanel;
    public TMP_Text hintText;
    public Button hintOpenButton;
    public Button hintNextButton;
    public Button hintCloseButton;

    [Header("닫기 / 다시하기")]
    public Button closeButton;
    public Button resetButton; // 풀다가 막히면 언제든 처음부터 다시 시작 (선택 사항)

    private readonly int[] _capacity = { 14, 9, 5 };
    private int[] _amount = new int[3];
    private bool _isPouring = false;
    private int _hintIndex = 0;

    private readonly string[] _hints =
    {
        "힌트 1\n처음부터 7L를 만들려고 하지 말고,\n9L 양동이에 남는 물의 양을 관찰해 보자.",
        "힌트 2\n9L 양동이를 가득 채운 뒤, 5L 양동이로 옮기면\n9L 양동이에는 4L가 남는다.\n이 4L가 다음 풀이의 실마리가 된다.",
        "힌트 3\n5L 양동이를 비운 뒤, 9L 양동이에 남은 물을\n다시 5L 양동이에 옮겨 보자.\n그 다음 14L 양동이로 9L 양동이를 다시 가득 채우면,\n5L 양동이에 남아 있던 물 때문에 새로운 양이 만들어진다."
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (hintOpenButton != null) hintOpenButton.onClick.AddListener(ShowHintPanel);
        if (hintNextButton != null) hintNextButton.onClick.AddListener(NextHint);
        if (hintCloseButton != null) hintCloseButton.onClick.AddListener(CloseHintPanel);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (resetButton != null) resetButton.onClick.AddListener(ResetPuzzle);

        if (panel != null) panel.SetActive(false);
        if (hintPanel != null) hintPanel.SetActive(false);
    }

    public void Open()
    {
        _amount = new int[] { 14, 0, 0 };
        _isPouring = false;
        if (hintPanel != null) hintPanel.SetActive(false);
        RefreshVisuals();
        if (panel != null) panel.SetActive(true);
    }

    // 언제든 닫기 가능 - 다시 열면 Open()에서 자동으로 처음부터 리셋됨
    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    // 닫지 않고 그 자리에서 처음부터 다시 시작
    public void ResetPuzzle()
    {
        if (_isPouring) return;
        _amount = new int[] { 14, 0, 0 };
        RefreshVisuals();
    }

    // ===== 드래그&드롭 붓기 =====

    public void OnBucketDropped(int from, int to)
    {
        if (_isPouring) return;
        if (from == to) return;
        if (_amount[from] <= 0) return;
        if (_amount[to] >= _capacity[to]) return;

        StartCoroutine(PourAnimation(from, to));
    }

    private IEnumerator PourAnimation(int from, int to)
    {
        _isPouring = true;

        int amount = Mathf.Min(_amount[from], _capacity[to] - _amount[to]);
        int startFrom = _amount[from];
        int startTo = _amount[to];
        int endFrom = startFrom - amount;
        int endTo = startTo + amount;

        RectTransform fromRect = bucketButtons[from] != null ? bucketButtons[from].GetComponent<RectTransform>() : null;
        Quaternion originalRot = fromRect != null ? fromRect.rotation : Quaternion.identity;

        if (bucketBackgrounds != null && from < bucketBackgrounds.Length && bucketBackgrounds[from] != null)
            bucketBackgrounds[from].color = pouringColor;

        // 1) 대상 방향으로 기울이기
        float dir = (to > from) ? -1f : 1f;
        Quaternion tiltRot = Quaternion.Euler(0, 0, dir * tiltAngle);
        yield return RotateOverTime(fromRect, originalRot, tiltRot, tiltDuration);

        // 2) 기울인 채로 물이 서서히 옮겨감
        float t = 0f;
        while (t < pourDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / pourDuration);
            float curFrom = Mathf.Lerp(startFrom, endFrom, p);
            float curTo = Mathf.Lerp(startTo, endTo, p);

            if (bucketFills[from] != null) bucketFills[from].fillAmount = curFrom / _capacity[from];
            if (bucketFills[to] != null) bucketFills[to].fillAmount = curTo / _capacity[to];
            if (amountTexts[from] != null) amountTexts[from].text = $"{Mathf.RoundToInt(curFrom)}L / {_capacity[from]}L";
            if (amountTexts[to] != null) amountTexts[to].text = $"{Mathf.RoundToInt(curTo)}L / {_capacity[to]}L";

            yield return null;
        }

        _amount[from] = endFrom;
        _amount[to] = endTo;

        // 3) 원래 각도로 복귀
        yield return RotateOverTime(fromRect, tiltRot, originalRot, tiltDuration);

        if (bucketBackgrounds != null && from < bucketBackgrounds.Length && bucketBackgrounds[from] != null)
            bucketBackgrounds[from].color = normalColor;

        RefreshVisuals();
        _isPouring = false;
        CheckWin();
    }

    private IEnumerator RotateOverTime(RectTransform rect, Quaternion from, Quaternion to, float duration)
    {
        if (rect == null) yield break;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            rect.rotation = Quaternion.Lerp(from, to, t / duration);
            yield return null;
        }
        rect.rotation = to;
    }

    private void CheckWin()
    {
        if (_amount[0] == 7 && _amount[1] == 7)
        {
            WaterPuzzleState.WaterSplit = true;
            Close();

            if (CenterMessageUI.Instance != null)
                CenterMessageUI.Instance.Show("정화수가 정확히 둘로 나뉘었다.");

            Debug.Log("정화수 퍼즐 성공! 14L/9L 양동이에 각각 7L씩 나뉨.");
        }
    }

    private void RefreshVisuals()
    {
        for (int i = 0; i < 3; i++)
        {
            if (amountTexts != null && i < amountTexts.Length && amountTexts[i] != null)
                amountTexts[i].text = $"{_amount[i]}L / {_capacity[i]}L";

            if (bucketFills != null && i < bucketFills.Length && bucketFills[i] != null)
                bucketFills[i].fillAmount = (float)_amount[i] / _capacity[i];

            if (bucketBackgrounds != null && i < bucketBackgrounds.Length && bucketBackgrounds[i] != null)
                bucketBackgrounds[i].color = normalColor;
        }
    }

    // ===== 힌트 =====

    private void ShowHintPanel()
    {
        _hintIndex = 0;
        if (hintText != null) hintText.text = _hints[_hintIndex];
        if (hintPanel != null) hintPanel.SetActive(true);
    }

    private void NextHint()
    {
        _hintIndex = Mathf.Min(_hintIndex + 1, _hints.Length - 1);
        if (hintText != null) hintText.text = _hints[_hintIndex];
    }

    private void CloseHintPanel()
    {
        if (hintPanel != null) hintPanel.SetActive(false);
    }
}
