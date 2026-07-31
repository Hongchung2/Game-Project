using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 가을방 주술사 NPC. 문제문 팝업 표시, 청동방울 반납 시 가을 추(秋) 족자 지급 후 다음 방으로 전송.
public class ShamanNPC : MonoBehaviour, IInteractable
{
    [Header("다음 방 전환")]
    public string nextSceneName = ""; // 겨울방 씬이 아직 없으면 비워둠
    public float fadeDuration = 0.5f;

    private const string QuestText =
        "이 방의 주인인 주술사는 오래된 청동 방울을 지키고 있었다.\n" +
        "그러나 먹그림 속에서 태어난 묵령 하나가 방울을 훔쳐 먹그림자 미로 안으로 달아났다.\n" +
        "방울을 빼앗긴 주인은 묵령을 놓치기 직전, 미로에 주술을 걸었다.\n" +
        "주술에 걸린 묵령은 마음대로 도망칠 수 없다.\n" +
        "묵령은 시작점의 화살표로 도망쳤으며, 다음의 규칙을 어길 수 없다.\n\n" +
        "- U턴 하거나 역주행 할 수 없다.\n" +
        "- 한 번 지나간 길은 다시 지날 수 없다.\n" +
        "- 갈림길에서는 반드시 좌회전 또는 우회전 해야 한다.\n" +
        "- 갈림길에서 직진할 수 없다.\n" +
        "- 주술 때문에 정확히 7번만 방향을 바꿀 수 있다.\n" +
        "- 3번째 방향 전환은 반드시 좌회전이어야 한다.\n" +
        "- 6번째 방향 전환은 반드시 우회전이어야 한다.\n\n" +
        "먹그림자 미로에는 '가'부터 '사'까지 총 7개의 출구가 있다.\n" +
        "이 규칙을 모두 만족하면서 묵령이 도달할 수 있는 출구는 어디인가?\n" +
        "묵령의 도주로를 밝혀내고, 청동 방울을 되찾아 방의 주인에게 돌려주어야 한다.";

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
            if (QuestPopupUI.Instance != null)
                QuestPopupUI.Instance.Show(QuestText);
        }
    }

    private IEnumerator ReturnBellSequence()
    {
        BronzeBellState.HasBell = false;

        ScrollCollection.Collect("fall");
        Debug.Log("청동 방울을 돌려주고 가을 추(秋) 족자를 받았다!");

        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeOut(Color.black, fadeDuration);

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
            yield break;
        }

        Debug.Log("다음 방(겨울방) 씬이 아직 없습니다. nextSceneName을 설정하면 전환됩니다.");

        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeIn(fadeDuration);
    }
}
