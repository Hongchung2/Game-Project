using System.Collections.Generic;
using UnityEngine;

// 보스방에 들어갈 때마다(씬 로드 시) 암석을 무작위 위치에 새로 배치한다.
// 전에는 에디터에서 고정 좌표로 미리 박아뒀는데, 매번 같은 자리라 지루하고 -
// 암석이 "한 번 부딪히면 부서져서 없어지는" 소모품이 된 지금은 재입장할 때마다 새로 채워줘야 함.
public class BossRoomRockSpawner : MonoBehaviour
{
    private const string ROCK_SPRITE_PATH = "Art/암석";
    private const float ROCK_SCALE = 2.2f;
    private const float ROCK_COLLIDER_RADIUS = 0.1837748f;
    private const float MIN_SPACING = 6f; // 암석끼리 최소 간격
    private const float AVOID_RADIUS = 5f; // 플레이어/보스 시작지점 주변엔 안 놓음
    private const int MAX_ATTEMPTS_PER_ROCK = 30;

    public float roomWidth = 78f;
    public float roomHeight = 42f;
    public float wallMargin = 6f;
    public int rockCount = 14;
    public Vector2[] avoidPoints; // 플레이어 스폰, 본체 스폰, 페이즈2 스폰 지점 등

    private void Start()
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(ROCK_SPRITE_PATH);
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning($"[BossRoomRockSpawner] 암석 스프라이트를 찾지 못함: Resources/{ROCK_SPRITE_PATH}");
            return;
        }
        Sprite rockSprite = sprites[0];

        var placed = new List<Vector2>();

        for (int i = 0; i < rockCount; i++)
        {
            Vector2 pos;
            if (!TryFindSpot(placed, out pos)) continue;

            placed.Add(pos);
            SpawnRock(pos, rockSprite);
        }
    }

    private bool TryFindSpot(List<Vector2> placed, out Vector2 result)
    {
        for (int attempt = 0; attempt < MAX_ATTEMPTS_PER_ROCK; attempt++)
        {
            float x = Random.Range(wallMargin, roomWidth - wallMargin);
            float y = Random.Range(wallMargin, roomHeight - wallMargin);
            Vector2 candidate = new Vector2(x, y);

            bool ok = true;

            foreach (var p in placed)
            {
                if (Vector2.Distance(candidate, p) < MIN_SPACING) { ok = false; break; }
            }

            if (ok && avoidPoints != null)
            {
                foreach (var a in avoidPoints)
                {
                    if (Vector2.Distance(candidate, a) < AVOID_RADIUS) { ok = false; break; }
                }
            }

            if (ok)
            {
                result = candidate;
                return true;
            }
        }

        result = Vector2.zero;
        return false;
    }

    private void SpawnRock(Vector2 pos, Sprite rockSprite)
    {
        GameObject rock = new GameObject("Rock");
        rock.transform.position = pos;
        rock.transform.localScale = Vector3.one * ROCK_SCALE;

        var sr = rock.AddComponent<SpriteRenderer>();
        sr.sprite = rockSprite;
        sr.sortingOrder = 2;

        var col = rock.AddComponent<CircleCollider2D>();
        col.radius = ROCK_COLLIDER_RADIUS;

        rock.AddComponent<BossRockMarker>();
    }
}
