using UnityEngine;

public class AutoShootingRangeGenerator : MonoBehaviour
{
    [Header("사격장 크기 설정")]
    public float groundSize = 50f;
    public int targetCount = 10;

    [Header("프리팹")]
    public GameObject targetPrefab; // Cylinder나 직접 만든 Target Prefab 연결

    void Start()
    {
        CreateGround();
        CreateWalls();
        CreateTargets();
        CreateStartMarker();
    }

    void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.localScale = new Vector3(groundSize / 10, 1, groundSize / 10);
        ground.name = "Ground";
        ground.GetComponent<Renderer>().material.color = new Color(0.4f, 0.4f, 0.4f);
    }

    void CreateWalls()
    {
        float wallHeight = 4f;
        float wallThickness = 1f;
        float half = groundSize / 2;

        for (int i = 0; i < 4; i++)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.localScale = new Vector3(
                (i % 2 == 0) ? groundSize : wallThickness,
                wallHeight,
                (i % 2 == 0) ? wallThickness : groundSize
            );

            wall.transform.position = new Vector3(
                (i == 1) ? half : (i == 3 ? -half : 0),
                wallHeight / 2,
                (i == 0) ? half : (i == 2 ? -half : 0)
            );

            wall.name = "Wall_" + i;
            wall.GetComponent<Renderer>().material.color = new Color(0.6f, 0.6f, 0.6f);
        }
    }

    void CreateTargets()
    {
        if (targetPrefab == null)
        {
            // Cylinder 기본 표적 자동 생성
            for (int i = 0; i < targetCount; i++)
            {
                GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                target.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
                float x = Random.Range(-groundSize / 3, groundSize / 3);
                float z = Random.Range(5f, groundSize / 2 - 5f);
                target.transform.position = new Vector3(x, 1f, z);
                target.name = "Target_" + i;
                target.GetComponent<Renderer>().material.color = Color.red;
            }
        }
        else
        {
            // 연결된 프리팹을 이용해서 자동 배치
            for (int i = 0; i < targetCount; i++)
            {
                float x = Random.Range(-groundSize / 3, groundSize / 3);
                float z = Random.Range(5f, groundSize / 2 - 5f);
                Instantiate(targetPrefab, new Vector3(x, 1f, z), Quaternion.identity);
            }
        }
    }

    void CreateStartMarker()
    {
        GameObject start = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        start.transform.localScale = new Vector3(1, 0.1f, 1);
        start.transform.position = new Vector3(0, 0.1f, -groundSize / 2 + 5);
        start.GetComponent<Renderer>().material.color = Color.green;
        start.name = "PlayerStartPoint";
    }

}
