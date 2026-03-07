using UnityEngine;

public class Util
{
    public static T GetOrAddComponent<T>(GameObject go) where T : UnityEngine.Component
    {
        T component = go.GetComponent<T>();
        if (component == null)
            component = go.AddComponent<T>();
        return component;
    }

    public static GameObject FindChild(GameObject go, string name = null, bool recursive = false)
    {
        Transform transform = FindChild<Transform>(go, name, recursive);
        if (transform != null)
        {
            return transform.gameObject;
        }

        return null;

    }
    public static T FindChild<T>(GameObject go, string name = null, bool recursive = false) where T : UnityEngine.Object
    { // 언제 어디서든 쓸 수 있게 '자식 찾기' 전용 도구 정의
        if (go == null)
            return null;

        if (recursive == false) // 바로 밑에 있는 자식들을 찾아봄
        {
            for (int i = 0; i < go.transform.childCount; i++) // i번째 자식을 하나씩 꺼내옴
            {
                Transform transform = go.transform.GetChild(i);


                if (string.IsNullOrEmpty(name) || transform.name == name) // 현재 이름이 비어있거나, 내가 찾는 그 이름이랑 똑같다면
                {
                    T component = transform.GetComponent<T>(); // 그 자식한테서 내가 원하는 컴포넌트를 가져옴
                }
            }
        }

        else // 만약 깊숙히 숨어 있다면 자식의 자식까지 찾아봄
        {
            foreach(T component in go.GetComponentsInChildren<T>())
            {
                if (string.IsNullOrEmpty(name) || component.name == name)
                    return component;
            }
        }
            return null;
    }
}
    
        
