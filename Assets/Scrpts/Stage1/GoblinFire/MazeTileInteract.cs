using UnityEngine;
using System.Collections;

public class MazeTileInteract : MonoBehaviour
{
    public GameObject interactButton;
    public GameObject GameCanvas;
    public float zoomOutSize = 15f;
    public float viewDuration = 5f;

    public Camera PlayerCamera;
    float _originalSize;
    bool _isViewing = false;

    void Start()
    {
        _originalSize = PlayerCamera.orthographicSize;
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
            if (interactButton != null)
            {
                interactButton.SetActive(false);
            }
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
        GameCanvas.SetActive(false);

        // 현재 카메라 위치 저장
        Vector3 originalPos = PlayerCamera.transform.position;

        // 카메라를 도깨비불 중심으로 이동
        Vector3 mazeCenter = transform.position;
        PlayerCamera.transform.position = new Vector3(mazeCenter.x, mazeCenter.y, originalPos.z);
        PlayerCamera.orthographicSize = zoomOutSize;


        yield return new WaitForSeconds(viewDuration);

        // 원래 크기로 복귀
        PlayerCamera.transform.position = originalPos;
        PlayerCamera.orthographicSize = _originalSize;

        GameCanvas.SetActive(true);
        _isViewing = false;
    }
}
