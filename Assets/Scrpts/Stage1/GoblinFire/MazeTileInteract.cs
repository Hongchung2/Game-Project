using UnityEngine;
using System.Collections;
using Cinemachine;

public class MazeTileInteract : MonoBehaviour
{
    public GameObject interactButton;
    public GameObject GameCanvas;
    public float zoomOutSize = 15f;
    public float viewDuration = 5f;

    CinemachineVirtualCamera _virtualCamera;
    float _originalSize;
    bool _isViewing = false;

    void Start()
    {
        _virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
        _originalSize = _virtualCamera.m_Lens.OrthographicSize;
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
        if (!_isViewing)
        {
            StartCoroutine(ZoomOutView());
        }
    }

    IEnumerator ZoomOutView()
    {
        _isViewing = true;
        

        // 줌아웃
        _virtualCamera.m_Lens.OrthographicSize = zoomOutSize;

        yield return new WaitForSeconds(viewDuration);

        // 원래 크기로 복귀
        _virtualCamera.m_Lens.OrthographicSize = _originalSize;

        _isViewing = false;

    }
}
