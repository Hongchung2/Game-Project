using UnityEngine;

public class SoundManager
{
    AudioSource _bgmSource;
    AudioSource _sfxSource;

    public void Init()
    {
        GameObject go = new GameObject("Sound");
        Object.DontDestroyOnLoad(go);

        _bgmSource = go.AddComponent<AudioSource>();
        _bgmSource.loop = true;

        _sfxSource = go.AddComponent<AudioSource>();
    }

    // BGM 재생
    public void PlayBgm(AudioClip clip)
    {
        if (_bgmSource.clip == clip) return;
        _bgmSource.clip = clip;
        _bgmSource.Play();
        _bgmSource.volume = _bgmSource.volume; // 현재 볼륨 유지
    }

    // 효과음 재생
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip);
    }

    // BGM 정지
    public void StopBGM()
    {
        _bgmSource.Stop();
    }


    // BGM&SFX Volume 수정
    public void SetBGMVolume(float volume)
    {
        _bgmSource.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        _sfxSource.volume = volume;
    }
}
