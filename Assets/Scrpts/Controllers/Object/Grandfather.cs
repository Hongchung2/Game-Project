using System.Collections;
using UnityEngine;

// 겨울방 할아버지 NPC. 대화 → 정화수 퍼즐 → 호수 정화 확인 → 겨울 족자 지급 → (전부 모았으면) 엔딩까지 담당.
public class Grandfather : MonoBehaviour, IInteractable
{
    private const string QuestText =
        "이 방 가운데에서 시들시들한 나무를 돌보던 할아버지가 말했다.\n\n" +
        "\"몬스터들이 서쪽과 동쪽 호수를 오염시켰다네.\n" +
        "정화하려면 내가 가진 정화수를 정확히 반으로 나눠서\n" +
        "양쪽 호수에 균일하게 부어야 해. 호수는 예민해서\n" +
        "비율이 맞지 않으면 아무 일도 일어나지 않는다네.\n\n" +
        "도와준다면 탈출에 필요한 겨울 동(冬) 족자를 주겠네.\"\n\n" +
        "문제문\n" +
        "정화수가 가득 찬 14리터 양동이와 비어 있는 9리터, 5리터 양동이가 있다.\n" +
        "세 양동이에는 눈금이 없다.\n" +
        "다른 도구를 사용하지 않고, 정화수를 버리지 않은 채\n" +
        "양동이 사이에만 정화수를 옮겨야 한다.\n" +
        "정화수를 옮길 때는 반드시 한 양동이가 비거나,\n" +
        "다른 양동이가 가득 찰 때까지 부어야 한다.\n\n" +
        "14리터 양동이와 9리터 양동이에 각각 7리터씩 나누어 담아라.";

    public string GetInteractText()
    {
        if (ScrollCollection.IsCollected("winter")) return "F - 인사하기";
        if (LakePurifyState.AllPurified) return "F - 겨울 족자 받기";
        return "F - 대화하기";
    }

    public void OnInteract()
    {
        if (ScrollCollection.IsCollected("winter"))
        {
            Debug.Log("할아버지: 고맙네, 자네 덕분에 나무가 다시 건강해졌어.");
            return;
        }

        if (LakePurifyState.AllPurified)
        {
            StartCoroutine(GiveScrollSequence());
            return;
        }

        if (!WaterPuzzleState.WaterSplit)
        {
            if (QuestPopupUI.Instance != null)
                QuestPopupUI.Instance.Show(QuestText, () => WaterPuzzleUI.Instance.Open());
            return;
        }

        Debug.Log("할아버지: 정화수는 나눴으니, 서쪽과 동쪽 호수의 몬스터를 처치하고 정화하고 오게나.");
    }

    private IEnumerator GiveScrollSequence()
    {
        ScrollCollection.Collect("winter");
        Debug.Log("겨울 동(冬) 족자를 받았다!");

        if (ScrollCollection.AllCollected())
            yield return StartCoroutine(EndingSequence());
    }

    private IEnumerator EndingSequence()
    {
        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeOut(Color.black, 1f);

        if (CenterMessageUI.Instance != null)
            CenterMessageUI.Instance.Show("탈출에 성공했습니다", 9999f);

        Debug.Log("게임 클리어! 탈출에 성공했습니다.");
    }
}
