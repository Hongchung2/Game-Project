using UnityEngine;

public class SummerPedestal : MonoBehaviour
{
    [Header("발판 정보")]
    public string pedestalId; // "hak", "turtle", "tiger", "dragon"

    private SpriteRenderer _sr;
    private Color _originColor;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _originColor = _sr.color;
    }

    // 석상이 정답 발판에 도착했을 때 호출
    public void OnStatueSealed()
    {
        Debug.Log($"{pedestalId} 발판 봉인 완료!");
    }
}
