using UnityEngine;

public class SadangDoorInteract : MonoBehaviour
{
    public GameObject interactButton; // 상호작용 버튼
    void Start()
    {
        interactButton.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            interactButton.SetActive(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            interactButton.SetActive(false);
        }
    }

    public void OnInteract()
    {
        PuzzleManager.Instance.OpenPuzzle();
        interactButton.SetActive(false);
    }
}
