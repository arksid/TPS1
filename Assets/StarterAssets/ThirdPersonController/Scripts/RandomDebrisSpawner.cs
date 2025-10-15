using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RandomDebrisSpawner : MonoBehaviour
{
    [Header("?? 배치할 프리팹들 (잔해물 등)")]
    public GameObject[] debrisPrefabs;

    [Header("?? 배치 범위 설정")]
    public Vector3 areaSize = new Vector3(100, 0, 100);
    public int spawnCount = 50;

    [Header("?? 회전 & 크기 랜덤 옵션")]
    public bool randomRotation = true;
    public bool randomScale = true;
    public Vector2 scaleRange = new Vector2(0.8f, 1.5f);

    [Header("?? 배치 높이 & 지면맞춤")]
    public float raycastHeight = 50f;
    public LayerMask groundMask;

    [Header("?? 생성물 정리")]
    public Transform parentObject;
    public bool clearBeforeSpawn = true;

    [Header("▶ 실행시 다시 생성 안 함")]
    public bool spawnOnPlay = false;

#if UNITY_EDITOR
    [ContextMenu("?? 잔해 랜덤 배치 (에디터에서 실행)")]
    public void SpawnDebrisInEditor()
    {
        SpawnDebris();
    }
#endif

    void Start()
    {
        if (spawnOnPlay)
            SpawnDebris();
    }

    public void SpawnDebris()
    {
        if (debrisPrefabs == null || debrisPrefabs.Length == 0)
        {
            Debug.LogWarning("[RandomDebrisSpawner] 프리팹이 없습니다!");
            return;
        }

        if (parentObject == null)
        {
            GameObject p = new GameObject("DebrisParent");
            p.transform.SetParent(transform);
            parentObject = p.transform;
        }

        if (clearBeforeSpawn)
        {
#if UNITY_EDITOR
            // 에디터에서 실행 시 Undo 기록 남겨서 되돌리기 가능
            Undo.RegisterFullObjectHierarchyUndo(parentObject.gameObject, "Clear Debris");
#endif
            for (int i = parentObject.childCount - 1; i >= 0; i--)
            {
#if UNITY_EDITOR
                Undo.DestroyObjectImmediate(parentObject.GetChild(i).gameObject);
#else
                Destroy(parentObject.GetChild(i).gameObject);
#endif
            }
        }

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 randomPos = GetRandomPositionInArea();
            Vector3 spawnPos = randomPos;

            // 지면 맞춤
            Ray ray = new Ray(randomPos + Vector3.up * raycastHeight, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, raycastHeight * 2, groundMask))
                spawnPos = hit.point;

            GameObject prefab = debrisPrefabs[Random.Range(0, debrisPrefabs.Length)];
#if UNITY_EDITOR
            GameObject debris = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parentObject);
            debris.transform.position = spawnPos;
#else
            GameObject debris = Instantiate(prefab, spawnPos, Quaternion.identity, parentObject);
#endif

            // 회전 랜덤
            if (randomRotation)
                debris.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            // 크기 랜덤 (원본 로컬 스케일 유지)
            if (randomScale)
            {
                float s = Random.Range(scaleRange.x, scaleRange.y);
                debris.transform.localScale = prefab.transform.localScale * s;
            }
        }

        Debug.Log($"[RandomDebrisSpawner] 잔해물 {spawnCount}개 배치 완료 ?");
    }

    Vector3 GetRandomPositionInArea()
    {
        Vector3 center = transform.position;
        float x = Random.Range(-areaSize.x / 2f, areaSize.x / 2f);
        float z = Random.Range(-areaSize.z / 2f, areaSize.z / 2f);
        return new Vector3(center.x + x, center.y, center.z + z);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
        Gizmos.DrawCube(transform.position, areaSize);
    }
}
