using UnityEngine;
using System.Collections;

public class GlowTileManager : MonoBehaviour
{
    public float stayTime = 3f; // 켜져있는 시간
    public float blinkTime = 3f; // 깜빡이는 시간
    public float blinkInterval = 0.2f; // 깜빡 간격 

    GlowTileController[] _tiles;
    void Start()
    {
        _tiles = GetComponentsInChildren<GlowTileController>();
        // 처음엔 다 끄기
        foreach (var tile in _tiles)
        {
            tile.SetVisible(false);
        }
        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        while (true)
        {
            for (int i=0; i<_tiles.Length; i++)
            {
                int next = (i + 1) % _tiles.Length;

                // 현재 타일 켜기
                _tiles[i].SetVisible(true);

                // stayTime 대기
                yield return new WaitForSeconds(stayTime);

                // 다음 타일 켜기
                _tiles[next].SetVisible(true);

                // 현재 타일 깜빡이고 끄기
                yield return StartCoroutine(_tiles[i].Blink(blinkTime, blinkInterval));
                _tiles[i].SetVisible(false);
            }
        }
    }
}
