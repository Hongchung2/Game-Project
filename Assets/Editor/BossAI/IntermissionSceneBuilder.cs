#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.Video;

// 작업지시서 #09 - 인터미션 씬(#08)을 코드로 생성. 영상 하나 재생하고 끝나면(또는 스킵하면) 보스방으로 전환.
public static class IntermissionSceneBuilder
{
    private const string SCENE_PATH = "Assets/Scenes/IntermissionScene.unity";
    private const string VIDEO_PATH = "Assets/Resources/Videos/Intermission.mp4";
    private const string BOSS_SCENE_PATH = "Assets/Scenes/Stage5_BossScene.unity";

    [MenuItem("Tools/Boss/Build Intermission Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Camera cam = BuildCamera();
        BuildEventSystem();
        BuildScreenFader();
        VideoPlayer videoPlayer = BuildVideoPlayer(cam);
        BuildSkipHint();
        BuildController(videoPlayer);

        EditorSceneManager.SaveScene(scene, SCENE_PATH);

        EnsureSceneInBuildSettings(SCENE_PATH);
        EnsureSceneInBuildSettings(BOSS_SCENE_PATH);

        Debug.Log($"[IntermissionSceneBuilder] {SCENE_PATH} 생성 완료 (Build Settings에도 등록됨)");
    }

    private static Camera BuildCamera()
    {
        // AudioListener 빠뜨렸었음(플레이테스트에서 발견 - "no audio listeners" 경고 + 영상 소리 안 남).
        var camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGO.tag = "MainCamera";
        return camGO.GetComponent<Camera>();
    }

    // EventSystem이 없어서 EventSystem.current가 null이 되고, 전역 InputManager.OnUpdate()가
    // 매 프레임 NullReferenceException을 던지던 문제(플레이테스트에서 발견) - 다른 씬들처럼 추가.
    private static void BuildEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    private static void BuildScreenFader()
    {
        new GameObject("ScreenFader").AddComponent<ScreenFader>();
    }

    private static VideoPlayer BuildVideoPlayer(Camera cam)
    {
        var clip = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>(VIDEO_PATH);
        if (clip == null)
            Debug.LogWarning($"[IntermissionSceneBuilder] 영상 클립을 찾지 못함: {VIDEO_PATH}");

        var go = new GameObject("IntermissionVideo");
        var player = go.AddComponent<VideoPlayer>();
        player.playOnAwake = false; // IntermissionController가 Start()에서 직접 재생
        player.isLooping = false;
        player.source = VideoSource.VideoClip;
        player.clip = clip;
        player.renderMode = VideoRenderMode.CameraNearPlane;
        player.targetCamera = cam;
        player.aspectRatio = VideoAspectRatio.FitInside; // 화면 비율 안 맞아도 찌그러지지 않게

        var audioSource = go.AddComponent<AudioSource>();
        player.audioOutputMode = VideoAudioOutputMode.AudioSource;
        player.SetTargetAudioSource(0, audioSource);

        return player;
    }

    // 재생 중 계속 보이는 안내 텍스트 - 화면 우하단, 스킵 방법 안내용.
    private static void BuildSkipHint()
    {
        GameObject canvasGO = new GameObject("Canvas", typeof(RectTransform));
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        canvasGO.AddComponent<CanvasScaler>();

        GameObject textGO = new GameObject("SkipHintText");
        textGO.transform.SetParent(canvasGO.transform, false);

        var text = textGO.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 한글 표시를 위해 TMP 기본 폰트 대신 사용
        text.text = "Space - 건너뛰기";
        text.fontSize = 24;
        text.alignment = TextAnchor.LowerRight;
        text.color = new Color(1f, 1f, 1f, 0.8f);

        var rect = text.rectTransform;
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-30f, 30f);
        rect.sizeDelta = new Vector2(300f, 50f);
    }

    private static void BuildController(VideoPlayer videoPlayer)
    {
        var go = new GameObject("IntermissionController");
        var controller = go.AddComponent<IntermissionController>();
        controller.videoPlayer = videoPlayer;
        controller.nextSceneName = "Stage5_BossScene";
    }

    private static void EnsureSceneInBuildSettings(string path)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == path)) return;

        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
