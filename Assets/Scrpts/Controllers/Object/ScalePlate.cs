using System.Collections.Generic;
using UnityEngine;

public class ScalePlate : MonoBehaviour
{
    public List<JarController> jarsOnPlate = new List<JarController>();
    private int maxJars = 3;

    private Vector3[] slotOffsets = new Vector3[]
    {
        new Vector3(-0.6f, 0.3f, 0),
        new Vector3(0f, 0.3f, 0),
        new Vector3(0.6f, 0.3f, 0)
    };

    public bool TryAddJar(JarController jar)
    {
        if (jarsOnPlate.Count >= maxJars) return false;

        if (!jarsOnPlate.Contains(jar))
        {
            jarsOnPlate.Add(jar);
            jar.transform.SetParent(transform);
            RealignJars();
            return true;
        }
        return false;
    }

    public void RemoveJar(JarController jar)
    {
        if (jarsOnPlate.Contains(jar))
        {
            jarsOnPlate.Remove(jar);
            RealignJars();
        }
    }

    private void RealignJars()
    {
        for (int i = 0; i < jarsOnPlate.Count; i++)
        {
            jarsOnPlate[i].transform.position = transform.position + slotOffsets[i];
        }
    }

    // [추가] 초기화할 때 접시의 메모리를 깨끗하게 비워줍니다!
    public void ClearPlate()
    {
        jarsOnPlate.Clear();
    }
}