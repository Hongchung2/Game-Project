using Mono.Cecil;
using UnityEngine;

public class ResourceManager
{
    public ResourceManager() { }

    // poolmanager가 이미 들고 있는 원본이 있는지 확인
    public T Load<T>(string path) where T : Object
    {
        if (typeof(T) == typeof(GameObject))
        {
            string name = path;
            int index = name.LastIndexOf('/');
            if (index >= 0)
                name = name.Substring(index + 1);

            GameObject go = Managers.Pool.GetOriginal(name);
            if (go != null)
            {
                return go as T;
            }
        }
        return Resources.Load<T>(path);
    }
    // 오브젝트를 소환할 때 poolable 컴포넌트가 붙어 있는지 확인한다음 있으면
    // manager.pool.pop을 호출해 꺼내오고 아니면 새로 만듦
    public GameObject Instantiate(string path, Transform parent = null)
    {
        GameObject original = Load<GameObject>($"Prefabs/{path}");
        if (original == null)
        {
            Debug.Log($"Failed to load prefab : {path}");
            return null;
        }

        if (original.GetComponent<Poolable>() != null)
            return Managers.Pool.Pop(original, parent).gameObject;

        GameObject go = Object.Instantiate(original, parent);
        go.name = original.name;
        return go;
    }
    // poolable이 붙어 있으면 파괴하지 않고 pool에 다시 저장
    public void Destroy(GameObject go)
    {
        if (go != null)
        {
            return;
        }

        Poolable poolable = go.GetComponent<Poolable>();
        if (poolable != null)
        {
            Managers.Pool.Push(poolable);
            return;
        }

        Object.Destroy(go);
    }
}
