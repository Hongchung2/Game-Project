using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_Joystick : UI_Scene, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    enum GameObjects
    {
        JoystickBG,
        JoystickCursor
    }

    // 외부에서 참조할 방향 데이터
    public Vector2 MoveDir { get; private set; } = Vector2.zero;

    float _radius;  // 조이스틱이 움직일 수 있는 최대 반경
    Vector2 _touchPos; // 처음 터치한 위치

    public override void Init()
    {
        base.Init();
        Bind<GameObject>(typeof(GameObjects));

        // 배경 크기의 절반 정도로 반지름 설정 (프리팹 크기에 따라)
        _radius = GetObject((int)GameObjects.JoystickBG).GetComponent<RectTransform>().sizeDelta.y / 2.5f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _touchPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 touchPos = eventData.position;
        Vector2 diff = touchPos - _touchPos;

        // 반지름 안으로 거리 제한
        float distance = Mathf.Min(diff.magnitude, _radius);
        MoveDir = diff.normalized * (distance / _radius); // 0 ~ 1 사이 값으로 정규화

        // 실제 커서 이미지 움직여주기
        GetObject((int)GameObjects.JoystickCursor).transform.localPosition = MoveDir * _radius;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 손 떼면 초기화
        MoveDir = Vector2.zero;
        GetObject((int)GameObjects.JoystickCursor).transform.localPosition = Vector2.zero;
    }
}
