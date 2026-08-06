using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 탈출 족자에 닿았을 때: 바로 전환하지 않고 살짝 망설이는 텀을 준 뒤, 봄방에서 기다리는
// 돗가비가 "뭐해 바보야! 빨리 들어가!!" 하고 재촉하는 대사를 보여주고 나서 씬을 전환한다.
public class EscapePortalReachedTrigger : MonoBehaviour
{
    private const float HESITATE_DURATION = 1f;
    private const string PORTRAIT_PATH = "Art/돗가비1";

    private static readonly string[] TauntLines =
    {
        "뭐해 바보야! 빨리 들어가!!",
    };

    public string nextSceneName = "Stage2_SpringScene";
    public float fadeDuration = 0.5f;
    public Action onEscaped; // director가 탈출 시간제한 타이머를 멈추는 데 사용

    private bool _triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;

        _triggered = true;
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        onEscaped?.Invoke();
        StartCoroutine(EscapeSequence());
    }

    private IEnumerator EscapeSequence()
    {
        yield return new WaitForSeconds(HESITATE_DURATION);

        bool dialogueDone = false;
        BossDialogueBox.Show(TauntLines, LoadPortrait(), () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeOut(Color.black, fadeDuration);

        GameProgress.SetCheckpoint(nextSceneName);
        SceneManager.LoadScene(nextSceneName);
    }

    private static Sprite LoadPortrait()
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(PORTRAIT_PATH);
        return sprites != null && sprites.Length > 0 ? sprites[0] : null;
    }
}
