using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

// 작업지시서 #09 - 인터미션 씬(#08). 영상을 재생하고, 끝나거나 스킵하면 보스방으로 넘어간다.
public class IntermissionController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string nextSceneName = "Stage5_BossScene";
    public float fadeDuration = 0.5f;
    public KeyCode skipKey = KeyCode.Space;

    private bool _transitioning = false;

    private void Start()
    {
        // 작업지시서 #09 - 이 씬을 만든 원래 목적: 영상 재생 중에 클로드 API를 미리 불러와서
        // 보스방 입장 시 기다림 없이 바로 반영되게 함.
        BossDecisionPrefetch.BeginPrefetch();

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += _ => TransitionToNextScene();
            videoPlayer.Play();
        }
    }

    private void Update()
    {
        if (!_transitioning && Input.GetKeyDown(skipKey))
            TransitionToNextScene();
    }

    private void TransitionToNextScene()
    {
        if (_transitioning) return;
        _transitioning = true;
        StartCoroutine(TransitionSequence());
    }

    private IEnumerator TransitionSequence()
    {
        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeOut(Color.black, fadeDuration);

        GameProgress.SetCheckpoint(nextSceneName);
        SceneManager.LoadScene(nextSceneName);
    }
}
