using System.Collections;
using UnityEngine;

public class SniperEnemy : EnemyController
{
    [Header("Sniper Settings")]
    public float laserDuration = 0.2f;

    [Header("Line Renderer Settings")]
    public LineRenderer laserLine;
    public Color laserColor = Color.red;
    public float lineWidth = 0.03f;

    protected override void Start()
    {
        base.Start();

        // ✅ LineRenderer 자동 생성 (Inspector에서 안 붙여도 됨)
        if (laserLine == null)
        {
            GameObject lineObj = new GameObject("SniperLaserLine");
            lineObj.transform.SetParent(transform);
            lineObj.transform.localPosition = Vector3.zero;

            laserLine = lineObj.AddComponent<LineRenderer>();
            laserLine.material = new Material(Shader.Find("Sprites/Default"));
            laserLine.startColor = laserColor;
            laserLine.endColor = laserColor;
            laserLine.startWidth = lineWidth;
            laserLine.endWidth = lineWidth;
            laserLine.positionCount = 2;
            laserLine.sortingOrder = 10;
            laserLine.enabled = false;
        }
    }

    protected override void Shoot()
    {
        StartCoroutine(SniperRoutine());
    }

    private IEnumerator SniperRoutine()
    {
        if (shootingPoint == null)
        {
            Debug.LogWarning("SniperEnemy: shootingPoint가 할당되지 않았습니다.");
            yield break;
        }

        Vector3 startPos = shootingPoint.position;
        Vector3 endPos = startPos + shootingPoint.forward * 100f;

        laserLine.SetPosition(0, startPos);
        laserLine.SetPosition(1, endPos);
        laserLine.enabled = true;

        yield return new WaitForSeconds(laserDuration);

        laserLine.enabled = false;
    }
    protected override void Die()
    {
        base.Die(); // 부모에서 이미 dropSystem 호출 가능
        var dropSystem = GetComponent<EnemyDropSystem>();
        if (dropSystem != null) dropSystem.TryDropItemByWeight();
    }

}
