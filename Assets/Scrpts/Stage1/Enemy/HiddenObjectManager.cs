using UnityEngine;

public class HiddenObjectManager : MonoBehaviour
{
    public DoorController door;
    private int objectCount;
    void Start()
    {
        objectCount = transform.childCount;
        door.SetDoorLocked(true);
    }

    public void OnObjectDestroyed()
    {
        objectCount--;
        Debug.Log("남은 물건: " + objectCount);
        if (objectCount <= 0)
        {
            Debug.Log("문 열림");
            door.SetDoorLocked(false);
        }
    }
}
