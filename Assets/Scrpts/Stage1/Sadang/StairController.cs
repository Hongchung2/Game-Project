using UnityEngine;

public class StairController : MonoBehaviour
{
    public string sceneName; // 이동할 씬
    public AudioClip moveSound;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Managers.Sound.PlaySFX(moveSound);
            GameProgress.SetCheckpoint(sceneName);
            Managers.Load.StartFadeAndLoad(sceneName, "Stage1_UndergroundScene1");
        }
    }

}
