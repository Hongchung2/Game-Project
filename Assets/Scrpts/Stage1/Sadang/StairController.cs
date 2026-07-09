using UnityEngine;

public class StairController : MonoBehaviour
{
    public string sceneName; // 이동할 씬

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Managers.Load.StartFadeAndLoad(sceneName, "Stage1_UndergroundScene1");
        }
    }

}
