using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// 양동이를 드래그해서 다른 양동이 위에 드롭하면 물을 붓는다.
// WaterPuzzleUI의 각 Bucket_XX 오브젝트에 붙이고 bucketIndex만 맞춰주면 된다 (0=14L, 1=9L, 2=5L).
public class DraggableBucket : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int bucketIndex;

    private RectTransform _rect;
    private Canvas _canvas;
    private Vector2 _originalPos;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _originalPos = _rect.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        float scale = (_canvas != null) ? _canvas.scaleFactor : 1f;
        _rect.anchoredPosition += eventData.delta / scale;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        DraggableBucket target = FindTargetUnderPointer(eventData);

        // 드래그 위치는 항상 원래 자리로 되돌리고, 실제 붓기는 애니메이션으로 처리한다.
        _rect.anchoredPosition = _originalPos;

        if (target != null && target != this && WaterPuzzleUI.Instance != null)
            WaterPuzzleUI.Instance.OnBucketDropped(bucketIndex, target.bucketIndex);
    }

    private DraggableBucket FindTargetUnderPointer(PointerEventData eventData)
    {
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            DraggableBucket db = result.gameObject.GetComponentInParent<DraggableBucket>();
            if (db != null && db != this) return db;
        }
        return null;
    }
}
