using UnityEngine;

// 양쪽 호수가 모두 정화되면 병든 나무가 회복된 모습으로 바뀐다 (정식 아트 나오기 전까지 색상 변경으로 대체).
public class WitheredTreeRecovery : MonoBehaviour
{
    public Color healthyColor = new Color(0.4f, 0.8f, 0.3f, 1f);

    private SpriteRenderer _sr;
    private bool _recovered = false;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (_recovered) return;
        if (!LakePurifyState.AllPurified) return;

        _recovered = true;
        if (_sr != null) _sr.color = healthyColor;
        Debug.Log("병든 나무가 다시 건강해졌다!");
    }
}
