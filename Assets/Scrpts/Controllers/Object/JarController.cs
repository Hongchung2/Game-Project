using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JarController : MonoBehaviour, IInteractable
{
    [Header("항아리 정보")]
    public int jarId;
    public bool isPickedUp = false;
    public static bool isPlayerHoldingJar = false;

    private Transform playerTransform;
    private Vector3 startPosition;

    void Awake()
    {
        startPosition = transform.position;
    }

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    public string GetInteractText()
    {
        if (isPickedUp) return "내려놓기";
        else if (isPlayerHoldingJar) return "손이 부족함";
        else return "항아리 들기";
    }

    public void OnInteract()
    {
        if (playerTransform == null) return;

        if (!isPickedUp)
        {
            if (isPlayerHoldingJar) return;

            // [수정] 내가 저울판 위에 있었든, 정답 단상 위에 있었든 부모를 확인해서 목록에서 빼줍니다.
            ScalePlate currentPlate = GetComponentInParent<ScalePlate>();
            if (currentPlate != null) 
            {
                currentPlate.RemoveJar(this);

                // [기획 수정 1-2] 저울판 위에 있던 항아리를 유저가 들면, 즉시 저울을 수평(원래 위치)으로 돌립니다!
                if (SpringPuzzleManager.Instance != null)
                {
                    SpringPuzzleManager.Instance.ResetPlatePositions();
                    Debug.Log("[저울] 항아리가 탈착되어 실시간으로 수평 복구되었습니다.");
                }
            }

            AnswerPedestal currentPedestal = GetComponentInParent<AnswerPedestal>();
            if (currentPedestal != null) currentPedestal.RemoveJar(this);

            isPickedUp = true;

            isPlayerHoldingJar = true;
            Debug.Log($"{jarId}번 항아리를 들었습니다!");

            transform.SetParent(playerTransform);
            transform.localPosition = new Vector3(0, 1.2f, 0);
        }
        else
        {
            isPickedUp = false;
            isPlayerHoldingJar = false;

            // 주변에 저울판이나 정답 단상이 있는지 탐색합니다.
            ScalePlate nearbyPlate = null;
            AnswerPedestal nearbyPedestal = null;

            Collider2D[] cols = Physics2D.OverlapCircleAll(playerTransform.position, 2.5f);
            foreach (Collider2D col in cols)
            {
                // 1. 저울판이 있는지 체크
                if (col.GetComponent<ScalePlate>() != null) nearbyPlate = col.GetComponent<ScalePlate>();
                // 2. 정답 단상이 있는지 체크
                if (col.GetComponent<AnswerPedestal>() != null) nearbyPedestal = col.GetComponent<AnswerPedestal>();
            }

            // [우선순위 1] 주변에 정답 단상이 있고 비어있다면 단상에 착!
            if (nearbyPedestal != null && nearbyPedestal.placedJar == null)
            {
                nearbyPedestal.TryAddJar(this);
            }
            // [우선순위 2] 주변에 저울판이 있고 빈자리(3개 미만)가 있다면 저울판에 착!
            else if (nearbyPlate != null && nearbyPlate.jarsOnPlate.Count < 3)
            {
                Debug.Log($"저울판에 {jarId}번 항아리를 셋팅합니다!");
                nearbyPlate.TryAddJar(this);
            }
            else
            {
                if (nearbyPlate != null && nearbyPlate.jarsOnPlate.Count >= 3)
                    Debug.Log("접시가 꽉 차서 바닥에 내려놓습니다.");
                else
                    Debug.Log($"{jarId}번 항아리를 바닥에 내려놓았습니다.");

                transform.SetParent(null);
                transform.position = playerTransform.position;
            }
        }
    }

    public void ResetToOrigin()
    {
        isPickedUp = false;
        isPlayerHoldingJar = false;
        transform.SetParent(null);
        transform.position = startPosition;
    }
}