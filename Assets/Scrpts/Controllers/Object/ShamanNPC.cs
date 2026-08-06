using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 가을방 주술사 NPC. 문제문 팝업 표시, 청동방울 반납 시 가을 추(秋) 족자 지급 후 다음 방으로 전송.
public class ShamanNPC : MonoBehaviour, IInteractable
{
    [Header("다음 방 전환")]
    public string nextSceneName = ""; // 겨울방 씬이 아직 없으면 비워둠
    public float fadeDuration = 0.5f;

    // BossDialogueBox(초상화+타이핑 연출)로 통일 - 주술사 초상화는 아직 없어서 null로 넘김
    // (이미지 나오면 여기에 스프라이트만 연결하면 됨).
    private static readonly string[] QuestLines =
    {
        "이 방의 주인인 나는 오래된 청동 방울을 지키고 있었다네.",
        "그런데 먹그림 속에서 태어난 묵령 하나가 그 방울을 훔쳐 먹그림자 미로 안으로 달아나버렸어.",
        "방울을 놓치기 직전, 다시는 도망 못 가게 미로에 주술을 걸어두었지. 묵령은 이제 마음대로 움직일 수 없다네.",
        "묵령은 시작점의 화살표로 도망쳤고, 다음 규칙을 절대 어길 수 없어.\n\n" +
            "- U턴하거나 역주행할 수 없다.\n" +
            "- 한 번 지나간 길은 다시 지날 수 없다.\n" +
            "- 갈림길에서는 반드시 좌회전 또는 우회전해야 한다.\n" +
            "- 갈림길에서 직진할 수 없다.\n" +
            "- 정확히 7번만 방향을 바꿀 수 있다.\n" +
            "- 3번째 방향 전환은 반드시 좌회전, 6번째는 반드시 우회전이어야 해.",
        "미로에는 '가'부터 '사'까지 총 7개의 출구가 있네. 이 규칙을 모두 만족하며 묵령이 도달할 수 있는 출구는 어디겠나?",
        "묵령의 도주로를 밝혀내고, 청동 방울을 되찾아 나에게 돌려주게.",
    };

    public string GetInteractText()
    {
        return BronzeBellState.HasBell ? "F - 청동 방울 돌려주기" : "F - 대화하기";
    }

    public void OnInteract()
    {
        if (BronzeBellState.HasBell)
        {
            StartCoroutine(ReturnBellSequence());
        }
        else
        {
            BossDialogueBox.Show(QuestLines, null);
        }
    }

    private IEnumerator ReturnBellSequence()
    {
        BronzeBellState.HasBell = false;

        ScrollCollection.Collect("fall");
        Debug.Log("청동 방울을 돌려주고 가을 추(秋) 족자를 받았다!");
        if (CenterMessageUI.Instance != null)
            CenterMessageUI.Instance.Show("가을 추(秋) 족자를 얻었다.", 2f);

        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeOut(Color.black, fadeDuration);

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            GameProgress.SetCheckpoint(nextSceneName);
            SceneManager.LoadScene(nextSceneName);
            yield break;
        }

        Debug.Log("다음 방(겨울방) 씬이 아직 없습니다. nextSceneName을 설정하면 전환됩니다.");

        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeIn(fadeDuration);
    }
}
