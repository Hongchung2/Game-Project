using UnityEngine;

public class HiddenObjectManager : MonoBehaviour
{
    public DoorController door;
    public int totalObjects = 5;
    private int objectCount;
    void Start()
    {
        objectCount = totalObjects;
        door.SetDoorLocked(true);
    }

    public void OnObjectDestroyed()
    {
        objectCount--;
        if (objectCount <= 0)
        {
            Debug.Log("문 열림");
            door.SetDoorLocked(false);
        }
    }
}
