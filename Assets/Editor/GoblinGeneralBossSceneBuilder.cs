#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

// 스테이지1 보스 "도깨비 장군" 관련 씬/프리팹을 코드로 생성하는 에디터 전용 빌더.
// Stage5(산수원) 쪽 BossRoomSceneBuilder와 동일한 이유로 존재 - 좌표/컴포넌트 실수를
// 줄이려고 직접 YAML을 조립하는 대신 실제 에셋을 AssetDatabase로 정확히 참조해 배치한다.
// 메뉴 번호 순서대로 실행하면 됨: 1 -> 2 -> 3 -> 4.
public static class GoblinGeneralBossSceneBuilder
{
    private const string BODY_SPRITE_PATH = "Assets/Animation/Obstacle/KakaoTalk_20260515_162448299_01.png";
    private const string WEAPON_SPRITE_PATH = "Assets/Animation/GoblinSpear/Goblin_Weapon.png";
    private const string ZOMBIE_SPRITE_PATH = "Assets/Undead Survivor/Sprites/Enemy 0.png";
    private const string FLOOR_TILE_PATH = "Assets/Tilemap/Palette/Black_Tile_0.asset";
    private const string WALL_TILE_PATH = "Assets/Tilemap/Palette/Underground_Wall_0.asset";
    private const string PLAYER_PREFAB_PATH = "Assets/Resources/Prefabs/Player/Character.prefab";
    private const string HP_BAR_PREFAB_PATH = "Assets/Resources/Prefabs/UI/Scene/UI_PlayerHPBar.prefab";

    private const string CLONE_PREFAB_PATH = "Assets/Resources/Prefabs/Monster/Monster_GoblinGeneralCharger.prefab";
    private const string ANIMATOR_CONTROLLER_PATH = "Assets/Animation/Obstacle/GoblinGeneral.controller";

    private const string CORRIDOR_SCENE_PATH = "Assets/Scenes/Stage1_BossCorridor.unity";
    private const string BOSS_SCENE_PATH = "Assets/Scenes/Stage1_BossScene.unity";
    private const string UNDERGROUND2_SCENE_PATH = "Assets/Scenes/Stage1_UndergroundScene2.unity";

    private const int ROOM_WIDTH = 78; // 26 x 3
    private const int ROOM_HEIGHT = 42; // 14 x 3
    private const int CORRIDOR_WIDTH = 6;
    private const int CORRIDOR_LENGTH = 50;

    // ===================== 1. 도깨비 장군 프리팹 =====================

    [MenuItem("Tools/Boss/1. Build GoblinGeneral Charger Prefab")]
    public static void BuildChargerPrefab()
    {
        Sprite bodySprite = LoadFirstSprite(BODY_SPRITE_PATH);
        Sprite weaponSprite = LoadFirstSprite(WEAPON_SPRITE_PATH);
        if (bodySprite == null || weaponSprite == null)
        {
            Debug.LogError("[GoblinGeneralBossSceneBuilder] 본체/창 스프라이트를 찾지 못함 - 임포트 상태 확인 필요");
            return;
        }

        GameObject root = new GameObject("Monster_GoblinGeneralCharger");
        int monsterLayer = LayerMask.NameToLayer("Monster");
        root.layer = monsterLayer >= 0 ? monsterLayer : 11;
        root.tag = "Monster"; // PlayerController의 몬스터 탐지가 이 태그 기준
        root.transform.localScale = Vector3.one * 0.25f; // 768px 원본을 다른 몬스터와 비슷한 크기로

        var sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = bodySprite;
        sr.sortingOrder = 5;

        var col = root.AddComponent<CircleCollider2D>();
        col.isTrigger = true; // 도깨비 장군끼리 겹쳐도 안 부딪히게(물리 충돌 없이 트리거만)
        col.radius = 3.7f;

        var animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = GetOrCreateMinimalAnimatorController();

        var stat = root.AddComponent<Stat>();
        stat.base_MaxHp = 12; // 프리팹 기본값 = 분신(1히트로 정체 드러남). 본체는 씬에서 120으로 덮어씀.
        stat.base_Attack = 35;
        stat.base_Defense = 0; // 실제 무적 처리는 GoblinGeneralCharger가 런타임에 Defense를 직접 조작
        stat.base_MoveSpeed = 0f;
        stat.InitStats();

        // 창 스프라이트는 이미 손잡이 쪽(우하단 근처)에 커스텀 피벗이 잡혀 있어서(Goblin_Weapon.png.meta),
        // 이 위치는 몸에 "쥐는 지점"만 정하면 됨 - 회전은 그 피벗을 축으로 시계바늘처럼 돎(GoblinGeneralCharger.AimWeaponAtPlayer).
        GameObject weapon = new GameObject("Weapon");
        weapon.transform.SetParent(root.transform, false);
        weapon.transform.localPosition = new Vector3(1f, -0.3f, 0f);
        weapon.transform.localScale = Vector3.one * 0.5f;
        var weaponSr = weapon.AddComponent<SpriteRenderer>();
        weaponSr.sprite = weaponSprite;
        weaponSr.sortingOrder = 6;

        root.AddComponent<GoblinGeneralCharger>();

        PrefabUtility.SaveAsPrefabAsset(root, CLONE_PREFAB_PATH);
        Object.DestroyImmediate(root);

        Debug.Log($"[GoblinGeneralBossSceneBuilder] {CLONE_PREFAB_PATH} 생성 완료");
    }

    private static AnimatorController GetOrCreateMinimalAnimatorController()
    {
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ANIMATOR_CONTROLLER_PATH);
        if (existing != null) return existing;

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ANIMATOR_CONTROLLER_PATH);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);
        return controller;
    }

    // Multiple 스프라이트 모드로 임포트된 텍스처는 LoadAssetAtPath<Sprite>로 못 찾고(메인 에셋이
    // Texture2D라서) LoadAllAssetsAtPath로 서브 에셋 중 Sprite를 걸러야 함 - PoofEffect.cs와 같은 이유.
    private static Sprite LoadFirstSprite(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    }

    private static Sprite LoadNamedSprite(string path, string spriteName)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(s => s.name == spriteName);
    }

    private static Sprite[] LoadNamedSprites(string path, params string[] spriteNames)
    {
        var all = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToList();
        var result = new List<Sprite>();
        foreach (var name in spriteNames)
        {
            var sprite = all.FirstOrDefault(s => s.name == name);
            if (sprite != null) result.Add(sprite);
        }
        return result.ToArray();
    }

    // ===================== 2. 복도 씬 =====================

    [MenuItem("Tools/Boss/2. Build Stage1 Boss Corridor Scene")]
    public static void BuildCorridorScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildManagersBootstrap();

        var grid = new GameObject("Grid", typeof(Grid));
        BuildRectRoom(grid.transform, CORRIDOR_WIDTH, CORRIDOR_LENGTH, "Corridor");

        BuildLight();

        // 복도가 길어서 고정 카메라로는 다 안 보임 - 플레이어를 따라가는 카메라 필요.
        // Transform을 직접 자식으로 붙이면 플레이어가 죽어서 SetActive(false)될 때 카메라까지
        // 같이 꺼져버려 "No cameras rendering"이 뜨는 문제가 있어서, 자식 관계 없이
        // CameraFollow 스크립트로 매 프레임 위치만 따라가게 한다.
        GameObject player = InstantiatePlayer(new Vector2(CORRIDOR_WIDTH / 2f, 1.5f));

        var camGO = new GameObject("Main Camera", typeof(Camera));
        camGO.tag = "MainCamera";
        var cam = camGO.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        camGO.transform.position = new Vector3(CORRIDOR_WIDTH / 2f, 1.5f, -10f);
        var camFollow = camGO.AddComponent<CameraFollow>();
        camFollow.target = player != null ? player.transform : null;

        // 입구: Stage1_UndergroundScene2에서 넘어온 직후 위치. 출구: 복도 끝 -> 보스방.
        GameObject exitTrigger = new GameObject("ExitToBossRoom");
        exitTrigger.transform.position = new Vector3(CORRIDOR_WIDTH / 2f, CORRIDOR_LENGTH - 1.5f, 0f);
        var exitCol = exitTrigger.AddComponent<BoxCollider2D>();
        exitCol.isTrigger = true;
        exitCol.size = new Vector2(CORRIDOR_WIDTH, 1f);
        var portal = exitTrigger.AddComponent<ClearPortalTrigger>();
        portal.nextSceneName = "Stage1_BossScene";

        new GameObject("ScreenFader").AddComponent<ScreenFader>();

        EditorSceneManager.SaveScene(scene, CORRIDOR_SCENE_PATH);
        Debug.Log($"[GoblinGeneralBossSceneBuilder] {CORRIDOR_SCENE_PATH} 생성 완료");
    }

    // ===================== 3. 보스방 씬 =====================

    [MenuItem("Tools/Boss/3. Build Stage1 Boss Scene")]
    public static void BuildBossScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildManagersBootstrap();

        var grid = new GameObject("Grid", typeof(Grid));
        BuildRectRoom(grid.transform, ROOM_WIDTH, ROOM_HEIGHT, "BossRoom");

        BuildLight();

        // 방이 78x42로 커져서 고정 카메라로 전체를 담으면 캐릭터가 너무 작아 보임 - 다른 스테이지처럼
        // 플레이어를 따라다니는 카메라로 전환(복도 씬과 같은 이유로 부모-자식 대신 CameraFollow 사용).
        GameObject player = InstantiatePlayer(new Vector2(ROOM_WIDTH / 2f, 1.5f));
        var camGO = new GameObject("Main Camera", typeof(Camera));
        camGO.tag = "MainCamera";
        var cam = camGO.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4f; // 다른 스테이지 씬(Stage1_Scene 등)과 동일한 값
        camGO.transform.position = new Vector3(ROOM_WIDTH / 2f, 1.5f, -10f);
        var camFollow = camGO.AddComponent<CameraFollow>();
        camFollow.target = player != null ? player.transform : null;

        // 본체 - 프리팹 그대로 인스턴스화한 뒤 체력/isRealBoss만 본체용으로 덮어씀.
        var clonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CLONE_PREFAB_PATH);
        GoblinGeneralCharger bossCharger = null;
        if (clonePrefab == null)
        {
            Debug.LogError("[GoblinGeneralBossSceneBuilder] 도깨비 장군 프리팹이 없음 - 먼저 '1. Build GoblinGeneral Charger Prefab' 실행 필요");
        }
        else
        {
            GameObject bossGO = (GameObject)PrefabUtility.InstantiatePrefab(clonePrefab);
            bossGO.name = "GoblinGeneralBoss";
            bossGO.transform.position = new Vector3(ROOM_WIDTH / 2f, ROOM_HEIGHT - 6f, 0f);

            bossCharger = bossGO.GetComponent<GoblinGeneralCharger>();
            bossCharger.isRealBoss = true;

            Stat bossStat = bossGO.GetComponent<Stat>();
            bossStat.base_MaxHp = 120;
            bossStat.InitStats();
        }

        // 페이즈2 소환 지점 8곳(본체 재배치 자리 포함) - 방 안에 고르게 분산.
        Transform[] spawnPoints = new Transform[8];
        Vector2[] spawnLayout =
        {
            new Vector2(ROOM_WIDTH / 2f, ROOM_HEIGHT - 6f),
            new Vector2(9f, ROOM_HEIGHT - 9f), new Vector2(ROOM_WIDTH - 9f, ROOM_HEIGHT - 9f),
            new Vector2(9f, ROOM_HEIGHT / 2f), new Vector2(ROOM_WIDTH - 9f, ROOM_HEIGHT / 2f),
            new Vector2(ROOM_WIDTH / 2f - 18f, 9f), new Vector2(ROOM_WIDTH / 2f + 18f, 9f),
            new Vector2(ROOM_WIDTH / 2f, ROOM_HEIGHT / 2f + 3f),
        };
        for (int i = 0; i < 8; i++)
        {
            var markerGO = new GameObject($"Phase2Spawn_{i}");
            markerGO.transform.position = spawnLayout[i];
            spawnPoints[i] = markerGO.transform;
        }

        var directorGO = new GameObject("GoblinGeneralBossDirector");
        var director = directorGO.AddComponent<GoblinGeneralBossDirector>();
        director.bossCharger = bossCharger;
        director.clonePrefab = clonePrefab;
        director.zombieRunSprites = LoadNamedSprites(ZOMBIE_SPRITE_PATH, "Run 0", "Run 1", "Run 2", "Run 3");
        director.zombieHitSprite = LoadNamedSprite(ZOMBIE_SPRITE_PATH, "Hit");
        director.zombieDeadSprite = LoadNamedSprite(ZOMBIE_SPRITE_PATH, "Dead");
        director.bossPortraitSprite = LoadNamedSprite(BODY_SPRITE_PATH, "Portrait");
        director.escapePortalSprite = LoadFirstSprite("Assets/Resources/Art/족자_봄.png");
        director.phase2SpawnPoints = spawnPoints;
        director.nextSceneName = "Stage2_SpringScene";
        director.roomWidth = ROOM_WIDTH;
        director.roomHeight = ROOM_HEIGHT;
        if (bossCharger != null) bossCharger.director = director;

        // 암석은 매번 씬이 시작할 때 무작위로 새로 배치(부딪히면 부서져 없어지는 소모품이라
        // 고정 좌표로 미리 심어두면 재입장 시 계속 같은 자리가 비어있게 됨).
        var rockSpawnerGO = new GameObject("BossRoomRockSpawner");
        var rockSpawner = rockSpawnerGO.AddComponent<BossRoomRockSpawner>();
        rockSpawner.roomWidth = ROOM_WIDTH;
        rockSpawner.roomHeight = ROOM_HEIGHT;
        rockSpawner.rockCount = 14;
        var avoidPoints = new System.Collections.Generic.List<Vector2>
        {
            new Vector2(ROOM_WIDTH / 2f, 1.5f), // 플레이어 입장 지점
        };
        foreach (var sp in spawnLayout) avoidPoints.Add(sp); // 본체/페이즈2 소환 지점
        rockSpawner.avoidPoints = avoidPoints.ToArray();

        BuildBossPlayerUI();

        EditorSceneManager.SaveScene(scene, BOSS_SCENE_PATH);
        Debug.Log($"[GoblinGeneralBossSceneBuilder] {BOSS_SCENE_PATH} 생성 완료");
    }

    // Managers 싱글턴은 "@Managers" 오브젝트의 Awake()에서 최초 1회 초기화된다(Managers.cs).
    // 정상 플레이 흐름(GameTitle부터)이면 앞선 씬에서 이미 DontDestroyOnLoad로 살아있지만,
    // 이 씬만 단독으로 Play하면 Managers.Game이 null이라 GoblinGeneralCharger.UpdateIdle()에서
    // NullReferenceException이 남 - 다른 스테이지 씬들처럼 이 씬에도 최소 구성으로 넣어둔다.
    private static void BuildManagersBootstrap()
    {
        if (GameObject.Find("@Managers") != null) return;
        var go = new GameObject("@Managers");
        go.AddComponent<Managers>();
    }

    private static void BuildBossPlayerUI()
    {
        GameObject canvasGO = new GameObject("Canvas", typeof(RectTransform));
        int uiLayer = LayerMask.NameToLayer("UI");
        canvasGO.layer = uiLayer >= 0 ? uiLayer : 5;

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.referenceResolution = new Vector2(800, 600);

        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject eventSystemGO = new GameObject("EventSystem");
        eventSystemGO.AddComponent<EventSystem>();
        eventSystemGO.AddComponent<InputSystemUIInputModule>();

        var hpBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HP_BAR_PREFAB_PATH);
        if (hpBarPrefab != null) PrefabUtility.InstantiatePrefab(hpBarPrefab, canvasGO.transform);

        new GameObject("ScreenFader").AddComponent<ScreenFader>();
        new GameObject("CenterMessageUI").AddComponent<CenterMessageUI>();
    }

    // ===================== 4. Stage1_UndergroundScene2 출구 연결 =====================

    [MenuItem("Tools/Boss/4. Wire Stage1_UndergroundScene2 Exit")]
    public static void WireUndergroundScene2Exit()
    {
        var scene = EditorSceneManager.OpenScene(UNDERGROUND2_SCENE_PATH, OpenSceneMode.Single);

        GameObject existing = GameObject.Find("ExitToBossCorridor");
        if (existing != null) Object.DestroyImmediate(existing);

        GameObject marker = GameObject.Find("GoblinExitPoint");
        Vector3 pos = marker != null ? marker.transform.position : Vector3.zero;

        GameObject exitTrigger = new GameObject("ExitToBossCorridor");
        exitTrigger.transform.position = pos;
        var col = exitTrigger.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.5f, 1.5f);
        var portal = exitTrigger.AddComponent<ClearPortalTrigger>();
        portal.nextSceneName = "Stage1_BossCorridor";

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[GoblinGeneralBossSceneBuilder] Stage1_UndergroundScene2 출구 연결 완료 (GoblinExitPoint 위치 기준, 필요시 손으로 위치 조정)");
    }

    // ===================== 공용 헬퍼 =====================

    private static void BuildRectRoom(Transform gridParent, int width, int height, string namePrefix)
    {
        var floorGO = new GameObject($"{namePrefix}Floor", typeof(Tilemap), typeof(TilemapRenderer));
        floorGO.transform.SetParent(gridParent);
        floorGO.GetComponent<TilemapRenderer>().sortingOrder = 0;

        TileBase floorTile = AssetDatabase.LoadAssetAtPath<TileBase>(FLOOR_TILE_PATH);
        var floorTilemap = floorGO.GetComponent<Tilemap>();
        if (floorTile != null)
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    floorTilemap.SetTile(new Vector3Int(x, y, 0), floorTile);
        }
        else
        {
            Debug.LogWarning($"[GoblinGeneralBossSceneBuilder] 바닥 타일을 찾지 못함: {FLOOR_TILE_PATH}");
        }

        var wallGO = new GameObject($"{namePrefix}Wall", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
        wallGO.transform.SetParent(gridParent);
        wallGO.GetComponent<TilemapRenderer>().sortingOrder = 1;

        TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WALL_TILE_PATH);
        var wallTilemap = wallGO.GetComponent<Tilemap>();
        if (wallTile != null)
        {
            for (int x = -1; x <= width; x++)
            {
                wallTilemap.SetTile(new Vector3Int(x, -1, 0), wallTile);
                wallTilemap.SetTile(new Vector3Int(x, height, 0), wallTile);
            }
            for (int y = -1; y <= height; y++)
            {
                wallTilemap.SetTile(new Vector3Int(-1, y, 0), wallTile);
                wallTilemap.SetTile(new Vector3Int(width, y, 0), wallTile);
            }
        }
        else
        {
            Debug.LogWarning($"[GoblinGeneralBossSceneBuilder] 벽 타일을 찾지 못함: {WALL_TILE_PATH}");
        }
    }

    private static void BuildLight()
    {
        var lightGO = new GameObject("Global Light 2D", typeof(Light2D));
        var light = lightGO.GetComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.color = Color.white;
        light.intensity = 1f;
    }

    private static void BuildFixedCamera()
    {
        var camGO = new GameObject("Main Camera", typeof(Camera));
        camGO.tag = "MainCamera";
        var cam = camGO.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = ROOM_HEIGHT / 2f + 1f;
        camGO.transform.position = new Vector3(ROOM_WIDTH / 2f, ROOM_HEIGHT / 2f, -10f);
    }

    private static GameObject InstantiatePlayer(Vector2 pos)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_PREFAB_PATH);
        if (prefab == null)
        {
            Debug.LogWarning($"[GoblinGeneralBossSceneBuilder] 플레이어 프리팹을 찾지 못함: {PLAYER_PREFAB_PATH}");
            return null;
        }

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        player.transform.position = new Vector3(pos.x, pos.y, 0f);
        return player;
    }
}
#endif
