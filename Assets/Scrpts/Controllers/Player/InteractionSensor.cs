using UnityEngine;

public class InteractionSensor : MonoBehaviour
{
    [Header("감지 설정")]
    public float detectRadius = 1.0f; // 상호작용 감지 범위
    public LayerMask interactableLayer; // 감지할 레이어 (우리가 만든 Interactable 지정)

    private IInteractable currentTarget = null; // 현재 감지된 상호작용 대상

    void Update()
    {
        DetectObject();

        // 스페이스바가 공격과 겹치므로, 임시로 'F' 키로 상호작용을 테스트합니다!
        if (Input.GetKeyDown(KeyCode.F) && currentTarget != null)
        {
            currentTarget.OnInteract();
        }
    }

    private void DetectObject()
    {
        // 플레이어 위치를 중심으로 원형 범위를 그려서 Interactable 레이어를 가진 물체를 찾습니다.
        Collider2D col = Physics2D.OverlapCircle(transform.position, detectRadius, interactableLayer);

        if (col != null)
        {
            // 충돌한 물체에 IInteractable 인터페이스(스크립트)가 달려있는지 확인합니다.
            IInteractable interactable = col.GetComponent<IInteractable>();

            if (interactable != null)
            {
                // 새로운 대상을 감지했을 때 UI에 텍스트를 띄웁니다.
                if (currentTarget != interactable)
                {
                    currentTarget = interactable;
                    Debug.Log($"상호작용 가능: {currentTarget.GetInteractText()}");

                    if (InteractPromptUI.Instance != null)
                        InteractPromptUI.Instance.Show(col.transform);
                }
                return;
            }
        }

        // 감지 범위에서 벗어났을 때 대상을 비워줍니다.
        if (currentTarget != null)
        {
            currentTarget = null;
            Debug.Log("상호작용 대상이 범위를 벗어났습니다.");

            if (InteractPromptUI.Instance != null)
                InteractPromptUI.Instance.Hide();
        }
    }

    // 유니티 씬(Scene) 화면에서 감지 범위를 초록색 원으로 보여주는 편의 기능입니다.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}