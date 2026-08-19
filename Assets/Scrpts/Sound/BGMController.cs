using UnityEngine;

public class BGM : MonoBehaviour
{
    public AudioClip bgmClip;

    [UnityEngine.Range(0f, 1f)]
    public float volume = 1f;
    void Start()
    {
        Managers.Sound.PlayBgm(bgmClip);
        Managers.Sound.SetBGMVolume(volume);
    }
}
