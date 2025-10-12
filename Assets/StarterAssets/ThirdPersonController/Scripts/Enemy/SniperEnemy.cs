using System.Collections;
using UnityEngine;

public class SniperEnemy : EnemyController
{
    [Header("저격병 설정")]
    public float aimTime = 2f;         // 조준 시간
    public float shotDelay = 0.5f;     // 발사 전 지연 시간
    public LineRenderer laserLine;     // 조준 레이저
    private bool isAiming = false;

    protected override void Start()
    {
        base.Start();
    }

    protected override void Shoot()
    {
        if (!isAiming)
            StartCoroutine(SniperRoutine());
    }

    private IEnumerator SniperRoutine()
    {
        isAiming = true;

        // 조준 시작
        if (laserLine != null)
            laserLine.enabled = true;

        float timer = 0f;
        while (timer < aimTime)
        {
            timer += Time.deltaTime;

            if (playerTarget == null) break;

            // 플레이어 위치 추적해서 레이저 조준 유지
            Vector3 targetPos = playerTarget.position + Vector3.up * 1.2f;
            laserLine.SetPosition(0, shootingPoint.position);
            laserLine.SetPosition(1, targetPos);

            yield return null;
        }

        // 발사 딜레이
        yield return new WaitForSeconds(shotDelay);

        // 발사
        if (shootingPoint != null && projectilePrefab != null && playerTarget != null)
        {
            Vector3 targetDir = (playerTarget.position + Vector3.up * 1.2f - shootingPoint.position).normalized;
            shootingPoint.rotation = Quaternion.LookRotation(targetDir);

            GameObject bullet = Instantiate(projectilePrefab, shootingPoint.position, shootingPoint.rotation);
            var proj = bullet.GetComponent<EnemyProjectile>();
            if (proj != null)
                proj.Init(gameObject, targetDir, projectileSpeed, projectileDamage);

            // 충돌 방지
            Collider bulletCol = bullet.GetComponent<Collider>();
            if (bulletCol != null)
            {
                Collider[] enemyCols = GetComponentsInChildren<Collider>();
                foreach (var c in enemyCols)
                    if (c != null) Physics.IgnoreCollision(bulletCol, c, true);
            }

            Destroy(bullet, 5f);
        }

        if (laserLine != null)
            laserLine.enabled = false;

        isAiming = false;
    }
}
