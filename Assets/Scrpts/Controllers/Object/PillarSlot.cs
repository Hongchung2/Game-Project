using UnityEngine;

// 작업지시서 #09 - 겨울방 기둥 4개 중 하나. 왼쪽(seasonIndex 낮은 것)부터 순서대로만 상호작용 가능.
// 성공하면 해당 계절 족자를 "사용됨" 처리하고 PillarSequenceState에 기록한다.
// 건영님: 기둥을 배치한 뒤 Inspector에서 seasonIndex를 0~3으로 채우고(0=봄,1=여름,2=가을,3=겨울),
// GameObject의 Layer를 "Interactable"로 바꿔주세요 (F키 감지는 Tag가 아니라 Layer 기준입니다).
public class PillarSlot : MonoBehaviour, IInteractable
{
    [Tooltip("0=봄, 1=여름, 2=가을, 3=겨울")]
    [Range(0, 3)]
    public int seasonIndex;

    private static readonly string[] SeasonNames = { "봄", "여름", "가을", "겨울" };

    private const string OrderGuideText =
        "왼쪽부터 봄, 여름, 가을, 겨울 순서대로 족자를 끼워 넣어야 될 것 같다.";

    public string GetInteractText()
    {
        if (PillarSequenceState.IsFilled(seasonIndex)) return "";
        return $"F - {SeasonNames[seasonIndex]} 족자 끼우기";
    }

    public void OnInteract()
    {
        if (PillarSequenceState.IsFilled(seasonIndex)) return;

        if (!PillarSequenceState.CanFill(seasonIndex))
        {
            Debug.Log($"[PillarSlot] {SeasonNames[seasonIndex]} 기둥 순서 위반 - 다음으로 필요한 인덱스: {PillarSequenceState.NextRequiredIndex()}");
            if (CenterMessageUI.Instance != null)
                CenterMessageUI.Instance.Show(OrderGuideText, 3f);
            return;
        }

        string season = ScrollCollection.Seasons[seasonIndex];
        ScrollCollection.Use(season);
        PillarSequenceState.Fill(seasonIndex);
        Debug.Log($"[PillarSlot] {SeasonNames[seasonIndex]} 기둥 완료 ({seasonIndex + 1}/4). 다음 필요 인덱스: {PillarSequenceState.NextRequiredIndex()}");
    }
}
