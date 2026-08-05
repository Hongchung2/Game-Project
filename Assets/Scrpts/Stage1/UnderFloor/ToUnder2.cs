using UnityEngine;

public class ToUnder2 : MonoBehaviour
{
    public string sceneName; // 이동할 씬

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameProgress.SetCheckpoint(sceneName);
            Managers.Load.StartFadeAndLoad(sceneName, "Stage1_UndergroundScene2");
        }
    }

}
