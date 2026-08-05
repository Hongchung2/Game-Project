#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

// M3: 보스방(Stage5_BossScene) 30x30 빈 껍데기를 코드로 생성하는 에디터 전용 빌더.
// 직접 손으로 .unity YAML을 조립하는 대신, 실제 프리팹/타일 에셋을 AssetDatabase로 정확히
// 참조해서 배치한다 — 좌표/컴포넌트 실수를 줄이기 위함. 실행 후에도 Scene은 평범한 씬이라
// 에디터에서 자유롭게 손으로 수정 가능.
public static class BossRoomSceneBuilder
{
    private const string SCENE_PATH = "Assets/Scenes/Stage5_BossScene.unity";
    // 채널2(화선지, M8b)가 바닥 위에 "먹물"을 누적하는 컨셉이라 바닥 자체는 밝아야 함
    // (Black_Tile였을 땐 검정 위에 검정이 찍혀서 안 보이는 문제 - 플레이테스트에서 발견).
    // 기존 겨울방 한지 바닥 에셋을 재사용 - 화선지 느낌에 원래도 잘 맞음.
    private const string FLOOR_TILE_PATH = "Assets/Resources/Art/겨울한지바닥_기본_0.asset";
    private const string WALL_TILE_PATH = "Assets/Tilemap/벽.asset";
    private const string PLAYER_PREFAB_PATH = "Assets/Resources/Prefabs/Player/Character.prefab";
    private const string HP_BAR_PREFAB_PATH = "Assets/Resources/Prefabs/UI/Scene/UI_PlayerHPBar.prefab";
    private const string BOSS_HP_BAR_PREFAB_PATH = "Assets/Resources/Prefabs/UI/Scene/UI_BossHPBar.prefab";

    private const int ROOM_SIZE = 30; // 30x30, 셀 (0,0)~(29,29) = 월드 (0,0)~(30,30)

    // 명세서 좌표계는 y가 클수록 "하단"(입구 dy=29=하단, 보스 위치 dy=5=상단) — Unity 월드는
    // y가 클수록 위쪽이라 반대. worldY = 30 - dy로 반전(정중앙 dy=15 -> worldY=15로 그대로
    // 맞아떨어져서 이 해석이 맞다는 근거로 삼음). x는 반전 불필요(좌우 대칭 그대로).
    private static Vector2 DesignToWorld(float dx, float dy) => new Vector2(dx, ROOM_SIZE - dy);

    [MenuItem("Tools/Boss/Build Stage5 Boss Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var grid = new GameObject("Grid", typeof(Grid));

        BuildFloor(grid.transform);
        BuildWalls(grid.transform);
        BuildLight();
        BuildCamera();
        BuildMarkers();
        BuildPlayer();
        BuildBoss();
        BuildPlayerUI();

        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        Debug.Log($"[BossRoomSceneBuilder] {SCENE_PATH} 생성 완료");
    }

    // M6a: 이미 만들어진 씬을 다시 열어서 보스만 추가/교체(바닥/벽/카메라는 그대로 유지).
    [MenuItem("Tools/Boss/Add Boss To Stage5 Scene")]
    public static void AddBossToExistingScene()
    {
        var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

        GameObject existing = GameObject.Find("MukunSangun");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing);

        BuildBoss();

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[BossRoomSceneBuilder] MukunSangun 보스 추가/갱신 완료");
    }

    // 플레이테스트 대비: 실제 플레이 중 HP를 볼 수 있어야 안전판(HP60%)/전투길이 체감 확인이
    // 가능해서 추가. 다른 스테이지 씬(Canvas+EventSystem+UI_PlayerHPBar) 구성을 그대로 재현.
    [MenuItem("Tools/Boss/Add Player HP UI To Stage5 Scene")]
    public static void AddPlayerUIToExistingScene()
    {
        var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

        GameObject existingCanvas = GameObject.Find("Canvas");
        if (existingCanvas != null) UnityEngine.Object.DestroyImmediate(existingCanvas);
        GameObject existingEventSystem = GameObject.Find("EventSystem");
        if (existingEventSystem != null) UnityEngine.Object.DestroyImmediate(existingEventSystem);

        BuildPlayerUI();

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[BossRoomSceneBuilder] 플레이어 HP UI 추가 완료");
    }

    private static void BuildPlayerUI()
    {
        GameObject canvasGO = new GameObject("Canvas", typeof(RectTransform));
        canvasGO.layer = LayerMask.NameToLayer("UI");

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.referenceResolution = new Vector2(800, 600);

        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject eventSystemGO = new GameObject("EventSystem");
        eventSystemGO.AddComponent<EventSystem>();
        eventSystemGO.AddComponent<InputSystemUIInputModule>();

        InstantiateUIPrefab(HP_BAR_PREFAB_PATH, canvasGO.transform);
        // UI_BossHPBar는 transform.parent에서 Stat을 못 찾으면 FindWithTag("Monster")로 폴백함
        // (이 씬엔 몬스터 태그가 보스 하나뿐이라 그대로 연결됨) - 별도 배선 불필요.
        InstantiateUIPrefab(BOSS_HP_BAR_PREFAB_PATH, canvasGO.transform);

        // 작업지시서 #09: 보스 격파 시 GameEndingTrigger가 이 둘을 찾아서 씀
        // (ScreenFader/CenterMessageUI는 빈 오브젝트에 컴포넌트만 붙이면 캔버스를 알아서 만듦).
        new GameObject("ScreenFader").AddComponent<ScreenFader>();
        new GameObject("CenterMessageUI").AddComponent<CenterMessageUI>();
    }

    private static void InstantiateUIPrefab(string path, Transform parent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning($"[BossRoomSceneBuilder] UI 프리팹을 찾지 못함: {path} (직접 배치 필요)");
            return;
        }
        PrefabUtility.InstantiatePrefab(prefab, parent);
    }

    // 명세서 3.3 데미지 값은 보스 스크립트 내부에 있고, 여기서는 Stat/Collider2D/태그 등
    // "몬스터로서 존재하기 위한 최소 골격"만 만든다(체력은 MukunSangunController.Init()에서 설정).
    private static void BuildBoss()
    {
        GameObject bossGO = new GameObject("MukunSangun");
        bossGO.tag = "Monster"; // PlayerController의 OverlapCircleAll(Monster 태그) 판정용

        Transform phase1Marker = GameObject.Find("BossPhase1Position")?.transform;
        if (phase1Marker != null) bossGO.transform.position = phase1Marker.position;

        var sr = bossGO.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.1f, 0.1f, 0.1f); // 아트 없음 - 임의 placeholder 색
        sr.sortingOrder = 3;
        bossGO.transform.localScale = Vector3.one * 1.5f;

        var col = bossGO.AddComponent<CircleCollider2D>();
        col.radius = 0.75f;

        bossGO.AddComponent<Stat>();
        bossGO.AddComponent<MukunSangunController>();
    }

    private static void BuildFloor(Transform gridParent)
    {
        var floorGO = new GameObject("Floor", typeof(Tilemap), typeof(TilemapRenderer));
        floorGO.transform.SetParent(gridParent);
        floorGO.GetComponent<TilemapRenderer>().sortingOrder = 0;

        TileBase floorTile = AssetDatabase.LoadAssetAtPath<TileBase>(FLOOR_TILE_PATH);
        if (floorTile == null)
        {
            Debug.LogWarning($"[BossRoomSceneBuilder] 바닥 타일을 찾지 못함: {FLOOR_TILE_PATH} (직접 배치 필요)");
            return;
        }

        var tilemap = floorGO.GetComponent<Tilemap>();
        for (int x = 0; x < ROOM_SIZE; x++)
            for (int y = 0; y < ROOM_SIZE; y++)
                tilemap.SetTile(new Vector3Int(x, y, 0), floorTile);
    }

    // 벽 타일맵엔 Tilemap Collider 2D만 붙이면 충분함(체크리스트: Rigidbody2D/Composite Collider 불필요).
    private static void BuildWalls(Transform gridParent)
    {
        var wallGO = new GameObject("Wall", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
        wallGO.transform.SetParent(gridParent);
        wallGO.GetComponent<TilemapRenderer>().sortingOrder = 1;

        TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WALL_TILE_PATH);
        if (wallTile == null)
        {
            Debug.LogWarning($"[BossRoomSceneBuilder] 벽 타일을 찾지 못함: {WALL_TILE_PATH} (직접 배치 필요)");
            return;
        }

        var tilemap = wallGO.GetComponent<Tilemap>();
        for (int x = -1; x <= ROOM_SIZE; x++)
        {
            tilemap.SetTile(new Vector3Int(x, -1, 0), wallTile);
            tilemap.SetTile(new Vector3Int(x, ROOM_SIZE, 0), wallTile);
        }
        for (int y = -1; y <= ROOM_SIZE; y++)
        {
            tilemap.SetTile(new Vector3Int(-1, y, 0), wallTile);
            tilemap.SetTile(new Vector3Int(ROOM_SIZE, y, 0), wallTile);
        }
    }

    // URP 2D 렌더러 사용 확인됨(겨울방에 Global Light 2D 존재) — 없으면 스프라이트가 그대로 검게 나옴.
    private static void BuildLight()
    {
        var lightGO = new GameObject("Global Light 2D", typeof(Light2D));
        var light = lightGO.GetComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.color = Color.white;
        light.intensity = 1f;
    }

    private static void BuildCamera()
    {
        var camGO = new GameObject("Main Camera", typeof(Camera));
        camGO.tag = "MainCamera";
        var cam = camGO.GetComponent<Camera>();
        cam.orthographic = true;
        // 30 높이 + 여유 1(위아래 0.5씩). 명세서 9.1-4가 실측 필요하다고 명시한 값 — 플레이테스트로 재확인 예정.
        cam.orthographicSize = 15.5f;

        Vector2 center = DesignToWorld(15, 15);
        camGO.transform.position = new Vector3(center.x, center.y, -10f);
    }

    private static void BuildMarkers()
    {
        CreateMarker("EntrancePoint", 15, 29);
        CreateMarker("BossPhase1Position", 15, 5);
        CreateMarker("LeftOrbitCenter", 8, 7);
        CreateMarker("RightOrbitCenter", 22, 7);
        CreateMarker("Phase2LandingPoint", 15, 15);
    }

    private static void CreateMarker(string name, float dx, float dy)
    {
        var go = new GameObject(name);
        Vector2 pos = DesignToWorld(dx, dy);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
    }

    private static void BuildPlayer()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_PREFAB_PATH);
        if (prefab == null)
        {
            Debug.LogWarning($"[BossRoomSceneBuilder] 플레이어 프리팹을 찾지 못함: {PLAYER_PREFAB_PATH} (직접 배치 필요)");
            return;
        }

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Vector2 pos = DesignToWorld(15, 29); // 입구
        player.transform.position = new Vector3(pos.x, pos.y, 0f);
    }

    // M3 완료기준 확인용: 고정 와이드 카메라(orthographicSize=15.5)가 실제로 어떻게 보이는지
    // 사람 눈 대신 렌더텍스처로 캡처해서 스크린샷 파일로 남긴다. 배치모드(그래픽 있는 상태)에서만 동작.
    [MenuItem("Tools/Boss/Capture Stage5 Camera Screenshot")]
    public static void CaptureCameraScreenshot()
    {
        EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

        var camGO = GameObject.Find("Main Camera");
        if (camGO == null)
        {
            Debug.LogError("[BossRoomSceneBuilder] Main Camera를 찾지 못해 스크린샷을 캡처하지 못함");
            return;
        }

        Camera cam = camGO.GetComponent<Camera>();
        var rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;

        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        cam.Render();
        RenderTexture prevActive = RenderTexture.active;
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        cam.targetTexture = null;
        RenderTexture.active = prevActive;
        UnityEngine.Object.DestroyImmediate(rt);

        byte[] png = tex.EncodeToPNG();
        string outPath = System.IO.Path.Combine(Application.dataPath, "..", "boss_room_camera_check.png");
        System.IO.File.WriteAllBytes(outPath, png);
        UnityEngine.Object.DestroyImmediate(tex);

        Debug.Log($"[BossRoomSceneBuilder] 스크린샷 저장 완료: {outPath}");
    }
}
#endif
