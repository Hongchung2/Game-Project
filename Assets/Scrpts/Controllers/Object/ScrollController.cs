using UnityEngine;

public class ScrollController : MonoBehaviour, IInteractable
{
    public string GetInteractText()
    {
        return "F - 족자 습득";
    }

    public void OnInteract()
    {
        ScrollCollection.Collect("spring");
        ExitController.Instance.UnlockExit();
        gameObject.SetActive(false);
    }
}
