using UnityEngine;

public class RandomMapGenerator : MonoBehaviour
{
    [Header("?? 배치할 프리팹들 (건물, 잔해, 자동차 등)")]
    public GameObject[] prefabs;

    [Header("??? 맵 설정")]
    public int objectCount = 100;             // 배치할 프리팹 개수
    public Vector2 mapSize = new Vector2(100, 100); // 맵 크기

    [Header("? 랜덤 회전/크기 옵션")]
    public bool randomRotation = true;
    public Vector2 scaleRange = new Vector2(0.8f, 1.2f); // 크기 랜덤 범위

    [Header("?? NavMeshObstacle 자동 추가")]
    public bool addNavObstacle = true;

    [Header("?? 충돌 방지 (프리팹끼리 겹치지 않게)")]
    public float minSpacing = 3f;

    private int maxTriesPerObject = 30; // 무한 루프 방지

    void Start()
    {
        GenerateMap();
    }

    public void GenerateMap()
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("[RandomMapGenerator] 프리팹이 등록되어 있지 않습니다!");
            return;
        }

        for (int i = 0; i < objectCount; i++)
        {
            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            Vector3 spawnPos;
            int tries = 0;

            // 겹치지 않는 위치 찾기
            do
            {
                spawnPos = new Vector3(
                    Random.Range(-mapSize.x / 2, mapSize.x / 2),
                    0,
                    Random.Range(-mapSize.y / 2, mapSize.y / 2)
                );
                tries++;
            } while (Physics.CheckSphere(spawnPos, minSpacing) && tries < maxTriesPerObject);

            Quaternion rot = Quaternion.identity;
            if (randomRotation)
                rot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            GameObject obj = Instantiate(prefab, spawnPos, rot);

            // 크기 랜덤 조정
            float randomScale = Random.Range(scaleRange.x, scaleRange.y);
            obj.transform.localScale *= randomScale;

            // NavMeshObstacle 자동 추가 (원하는 경우)
            if (addNavObstacle)
            {
                if (obj.GetComponent<UnityEngine.AI.NavMeshObstacle>() == null)
                {
                    var obstacle = obj.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                    obstacle.carving = true;
                }
            }

            obj.transform.parent = this.transform;
        }

        Debug.Log($"[RandomMapGenerator] 총 {objectCount}개의 프리팹이 랜덤하게 배치되었습니다.");
    }
}
