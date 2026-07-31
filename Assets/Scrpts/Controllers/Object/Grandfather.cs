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

    private const string EscortText =
        "할아버지가 자네의 손을 잡고 성큼성큼 걸어\n" +
        "서쪽 호수로 가는 길목까지 데려다주었다.\n\n" +
        "\"이 문을 지나면 서쪽 호수로 갈 수 있다네.\n" +
        "그곳을 지키는 몬스터들을 전부 물리치고 나서\n" +
        "정화수를 호수에 뿌려주게.\"";

    private const string EastPortalText =
        "할아버지가 흐뭇한 표정으로 말했다.\n\n" +
        "\"서쪽 호수가 다시 맑아졌구먼!\n" +
        "이제 동쪽 호수도 같은 방법으로 정화해주게.\n" +
        "동쪽으로 가는 문도 열어두었다네.\"";

    [Header("퍼즐 완료 후 포탈로 데려가기")]
    public GameObject westPortal;   // 평소엔 비활성화 상태로 두고, 퍼즐 풀면 여기서 활성화함
    public GameObject eastPortal;   // 서쪽 호수 정화 후 중앙으로 돌아오면 활성화됨
    public float escortMoveSpeed = 3f;
    public float escortStartDelay = 1.5f; // "정화수가 정확히 둘로 나뉘었다" 메시지가 보일 시간
    public float centerReturnXThreshold = -15f; // 이 값보다 x가 크면 "중앙으로 돌아왔다"고 판단

    private bool _eastAnnounced = false;

    private void Update()
    {
        if (_eastAnnounced || eastPortal == null) return;
        if (!LakePurifyState.WestPurified) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // 서쪽 호수 정화 후 포탈을 타고 중앙으로 돌아오면(x가 확 커짐) 동쪽 포탈을 안내
        if (player.transform.position.x > centerReturnXThreshold)
        {
            _eastAnnounced = true;
            StartCoroutine(AnnounceEastPortalSequence());
        }
    }

    private IEnumerator AnnounceEastPortalSequence()
    {
        eastPortal.SetActive(true);

        bool closed = false;
        if (QuestPopupUI.Instance != null)
            QuestPopupUI.Instance.Show(EastPortalText, () => closed = true);
        else
            closed = true;

        yield return new WaitUntil(() => closed);
    }

    private void OnEnable()
    {
        WaterPuzzleState.OnSolved += HandlePuzzleSolved;
    }

    private void OnDisable()
    {
        WaterPuzzleState.OnSolved -= HandlePuzzleSolved;
    }

    private void HandlePuzzleSolved()
    {
        StartCoroutine(EscortToPortalSequence());
    }

    private IEnumerator EscortToPortalSequence()
    {
        yield return new WaitForSeconds(escortStartDelay);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || westPortal == null) yield break;

        PlayerController playerController = player.GetComponent<PlayerController>();
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();

        if (playerController != null) playerController.enabled = false;
        if (playerRb != null) playerRb.linearVelocity = Vector2.zero;

        // 포탈 트리거 범위 "밖", 포탈 앞까지만 이동 (도착하자마자 활성화하면 바로 밟혀서 워프되는 것 방지)
        float portalRadius = 1f;
        CircleCollider2D portalCollider = westPortal.GetComponent<CircleCollider2D>();
        if (portalCollider != null) portalRadius = portalCollider.radius;
        float clearance = portalRadius + 0.6f;

        Vector2 portalPos = westPortal.transform.position;
        Vector2 playerStart = player.transform.position;
        Vector2 approachDir = (playerStart - portalPos).normalized;
        if (approachDir == Vector2.zero) approachDir = Vector2.down;
        Vector2 playerStop = portalPos + approachDir * clearance;

        // 할아버지도 플레이어 옆으로 같이 이동 (포탈 바로 앞이 아니라 살짝 옆으로 비켜서 겹치지 않게)
        Vector2 grandfatherStart = transform.position;
        Vector2 sideOffset = new Vector2(-approachDir.y, approachDir.x) * 0.7f;
        Vector2 grandfatherStop = playerStop + sideOffset;

        float moveTime = Mathf.Max(
            Vector2.Distance(playerStart, playerStop),
            Vector2.Distance(grandfatherStart, grandfatherStop)) / escortMoveSpeed;

        float elapsed = 0f;
        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveTime;

            Vector2 playerPos = Vector2.Lerp(playerStart, playerStop, t);
            if (playerRb != null) playerRb.MovePosition(playerPos);
            else player.transform.position = playerPos;

            transform.position = Vector2.Lerp(grandfatherStart, grandfatherStop, t);

            yield return null;
        }
        if (playerRb != null) playerRb.MovePosition(playerStop);
        else player.transform.position = playerStop;
        transform.position = grandfatherStop;

        bool closed = false;
        if (QuestPopupUI.Instance != null)
            QuestPopupUI.Instance.Show(EscortText, () => closed = true);
        else
            closed = true;

        yield return new WaitUntil(() => closed);

        westPortal.SetActive(true);

        if (playerController != null) playerController.enabled = true;
    }

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
