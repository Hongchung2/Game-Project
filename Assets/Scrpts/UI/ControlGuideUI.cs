using UnityEngine;

public class ControlGuideUI : MonoBehaviour
{
    public GameObject guidePanel;
    public GameObject openPanel;
    void Start()
    {
        guidePanel.SetActive(false);
        openPanel.SetActive(true);
    }

    public void OpenGuide()
    {
        guidePanel.SetActive(true);
        openPanel.SetActive(false);
    }

    public void CloseGuide()
    {
        guidePanel.SetActive(false);
        openPanel.SetActive(true);
    }
}
