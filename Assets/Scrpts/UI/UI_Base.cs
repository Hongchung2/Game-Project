using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class UI_Base : MonoBehaviour
{
    protected Dictionary<Type, UnityEngine.Object[]> _objects = new Dictionary<Type, UnityEngine.Object[]>(); // 종류별로 물건을 정리해둠

    public abstract void Init();

    private void Start()
    {
        Init();
    }
    protected void Bind<T>(Type type) where T : UnityEngine.Object // 어떤 종류(T)든 상관없이 자동으로 찾아주는 함수
    {
        string[] names = Enum.GetNames(type); // enum에 있는 리스트들을 뽑아서 배열로 만든다

        UnityEngine.Object[] objects = new UnityEngine.Object[names.Length]; // 찾은 것들을 저장할 임시 변수를 초기화
        _objects.Add(typeof(T), objects); // 찾은 리스트를 만든 변수에 추가

        for (int i = 0; i < names.Length; i++)
        {
            if (typeof(T) == typeof(GameObject))
                objects[i] = Util.FindChild(gameObject, names[i], true);
            else
                objects[i] = Util.FindChild<T>(gameObject, names[i], true);

            if (objects[i] == null)
                Debug.Log($"Failed to bind({names[i]})");
        }
    }

    protected T Get<T>(int idx) where T : UnityEngine.Object
    {
        UnityEngine.Object[] objects = null;
        if (_objects.TryGetValue(typeof(T), out objects) == false)
            return null;

        return objects[idx] as T;

    }

    protected GameObject GetObject(int idx)
    {
        return Get<GameObject>(idx);
    }
    protected TextMeshProUGUI GetText(int idx)
    {
        return Get<TextMeshProUGUI>(idx);
    }

    protected Button GetButton(int idx)
    {
        return Get<Button>(idx);
    }

    protected Image GetImage(int idx)
    {
        return Get<Image>(idx);
    }

    public static void BindEvent(GameObject go, Action<PointerEventData> action, Define.UIEvent type = Define.UIEvent.Click)
    {
        UI_EventHandler evt = Util.GetOrAddComponent<UI_EventHandler>(go);

        switch (type)
        {
            case Define.UIEvent.Click:
                evt.OnClickHandler -= action;
                evt.OnClickHandler += action;
                break;

            case Define.UIEvent.Drag:
                evt.OnDragHandler -= action;
                evt.OnDragHandler += action;
                break;
        }
        evt.OnDragHandler += ((PointerEventData data) => { evt.gameObject.transform.position = data.position; });
    }
}
