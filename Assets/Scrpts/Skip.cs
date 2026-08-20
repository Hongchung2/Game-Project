using UnityEngine;

public class Skip : MonoBehaviour
{
    public Transform destination;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.transform.position = destination.position;
        }
    }
}
