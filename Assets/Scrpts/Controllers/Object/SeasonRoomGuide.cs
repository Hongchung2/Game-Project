using UnityEngine;
using UnityEngine.SceneManagement;

// 보스방 탈출 후 사계절 방 구간(봄부터 시작) - 돗가비가 각 방을 짧게 소개하고, 퍼즐을 자연스러운
// 말투로 설명해준다. 가을/겨울은 이미 NPC(주술사/할아버지)가 설명해주고 있어서 여기선 제외 -
// 봄/여름만 대상. 대사창은 BossDialogueBox를 그대로 재사용(도깨비 장군 전용이 아니라 범용 컴포넌트).
public static class SeasonRoomGuideLoader
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Stage2_SpringScene")
        {
            if (!GameProgress.SpringArrivalGuideShown) SeasonRoomGuide.PlaySpringArrival();
            if (!GameProgress.SpringPuzzleGuideShown) SeasonRoomGuide.ArmSpringPuzzleHint();
        }
        else if (scene.name == "Stage2_SummerScene")
        {
            if (!GameProgress.SummerPuzzleGuideShown) SeasonRoomGuide.ArmSummerPuzzleHint();
        }
    }
}

public static class SeasonRoomGuide
{
    private const string PORTRAIT_PATH = "Art/돗가비1";

    // 봄방 도착 - 사계절 방 구조를 짧게 소개.
    private static readonly string[] SpringArrivalLines =
    {
        "여기부터는 사계절의 방이다. 봄, 여름, 가을, 겨울 — 방마다 널 막아서는 장치가 하나씩 있지.",
        "일단 둘러볼까?",
    };

    // 봄방 퍼즐(저울/항아리) - 발견 힌트 + 설명.
    private static readonly string[] SpringPuzzleLines =
    {
        "여긴 아무래도 지혜를 써야 하는 곳인가 본데?",
        "오, 이건 예전에 내가 즐겨 하던 게임이지~",
        "항아리 9개 중에 딱 하나만 무게가 달라. 저 저울에 양쪽으로 최대 3개씩 올려서 재보라고.",
        "근데 딱 세 번만 잴 수 있어 — 저 횃불 세 개 보이지? 잴 때마다 하나씩 꺼질 거야.",
        "수상한 놈을 찾았으면 저 단상에 올리고, 무거운지 가벼운지 맞혀보라고.",
    };

    // 여름방 퍼즐(석상 밀기) - 발견 힌트 + 설명.
    private static readonly string[] SummerPuzzleLines =
    {
        "여긴 아무래도 지혜를 써야 하는 곳인가 본데?",
        "흐음, 여긴 몸으로 때우는 놀이구먼.",
        "저 석상들 보이지? F키 누르고 미는 방향으로 쭉 밀어봐 — 뭔가에 부딪힐 때까지 미끄러질 거야.",
        "각자 자기랑 똑같이 생긴 단상 위에 딱 맞춰서 세워야 해.",
        "어딘가 있는 스위치를 건드리면 길 막는 기둥들이 사라질 거야. 잘 찾아보라고.",
    };

    public static void PlaySpringArrival()
    {
        BossDialogueBox.Show(SpringArrivalLines, LoadPortrait(), GameProgress.MarkSpringArrivalGuideShown);
    }

    // 실제 퍼즐 오브젝트 근처(Puzzle_Objects)에 트리거를 심어뒀다가, 플레이어가 다가가면 그때 보여줌.
    public static void ArmSpringPuzzleHint()
    {
        ArmHintZone("Puzzle_Objects", SpringPuzzleLines, GameProgress.MarkSpringPuzzleGuideShown);
    }

    public static void ArmSummerPuzzleHint()
    {
        ArmHintZone("Puzzle_Objects", SummerPuzzleLines, GameProgress.MarkSummerPuzzleGuideShown);
    }

    private static void ArmHintZone(string anchorObjectName, string[] lines, System.Action onShown)
    {
        GameObject anchor = GameObject.Find(anchorObjectName);
        Vector3 pos = anchor != null ? anchor.transform.position : Vector3.zero;
        SeasonPuzzleHintZone.Arm(pos, 4f, lines, onShown);
    }

    public static Sprite LoadPortrait()
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(PORTRAIT_PATH);
        return sprites != null && sprites.Length > 0 ? sprites[0] : null;
    }
}

// 플레이어가 퍼즐 근처에 다가오면 한 번 대사를 띄우고 스스로 사라지는 트리거.
public class SeasonPuzzleHintZone : MonoBehaviour
{
    private string[] _lines;
    private System.Action _onShown;

    public static void Arm(Vector3 position, float radius, string[] lines, System.Action onShown)
    {
        GameObject go = new GameObject("SeasonPuzzleHintZone");
        go.transform.position = position;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = radius;
        var zone = go.AddComponent<SeasonPuzzleHintZone>();
        zone._lines = lines;
        zone._onShown = onShown;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        BossDialogueBox.Show(_lines, SeasonRoomGuide.LoadPortrait(), () => _onShown?.Invoke());
        Destroy(gameObject);
    }
}
