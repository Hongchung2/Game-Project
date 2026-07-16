using UnityEngine;

public class Detection : MonoBehaviour
{
    [Header("감지 범위")]

    [SerializeField]
    private float detectWidth = 6.0f;   

    [SerializeField]
    private float detectHeight = 2.0f;  

    [SerializeField]
    private float offsetX = 1.0f;       
     
    [SerializeField]
    private float offsetY = 0.5f;       

    [SerializeField]
    private LayerMask playerLayer;

    [Header("감지 색")]

    [SerializeField]
    private Color gizmoColor = new Color(1, 0, 0, 1.0f);

    public bool playerDetected { get; private set; } 
    public bool playerBack { get; private set; } 
    public Transform Player { get; private set; } 



    void Update()
    {
        DetectPlayer();
    }

    private void DetectPlayer() 
    {
        bool isFacingLeft = transform.localScale.x > 0;

        float xDirection = isFacingLeft ? -1f : 1f;

        Vector2 centerOffset = new Vector2(offsetX * xDirection, offsetY);
        Vector2 center = (Vector2)transform.position + centerOffset;

        Vector2 boxsize = new Vector2(detectWidth, detectHeight);

        Collider2D hit = Physics2D.OverlapBox(center, boxsize, 0f, playerLayer);
        playerDetected = (hit != null);

        if (playerDetected)
        {
            Player = hit.transform;

            bool isPlayerOnRight = Player.position.x > transform.position.x;

            if ((isFacingLeft && isPlayerOnRight) || (!isFacingLeft && !isPlayerOnRight)) 
            {
                playerBack = true;
            }
            else
            {
                playerBack = false;
            }
        }
        else
        {
            playerBack = false;
            Player = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = gizmoColor;
        bool isFacingLeft = transform.localScale.x > 0;
        float xDirection = isFacingLeft ? -1f : 1f;

        Vector2 centerOffset = new Vector2(offsetX * xDirection, offsetY);
        Vector2 center = (Vector2)transform.position + centerOffset;

        Gizmos.DrawWireCube(center, new Vector3(detectWidth, detectHeight, 1));
    }
}
