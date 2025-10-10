using UnityEngine;
using System.Collections;

public class MachineGunEnemy : EnemyController
{
    [Header("기관총 설정")]
    public int burstCount = 5;                 // 한 번에 쏘는 발수 (0이면 연속)
    public float timeBetweenBullets = 0.08f;   // 발사 간격 (연사속도)
    public float spreadAngle = 6f;             // 탄 퍼짐(도)

    private bool isShootingBurst = false;

    protected override void Start()
    {
        base.Start();
        // 기관총은 기본 쿨다운(타겟팅 간격)을 짧게 설정해도 됨
        // shootCooldown는 웨이브마다/발견 후 대기용으로 사용
    }

    protected override void Shoot()
    {
        // 기본 쿨타임 체크는 부모 Update가 함
        if (!isShootingBurst)
            StartCoroutine(BurstRoutine());
    }

    private IEnumerator BurstRoutine()
    {
        isShootingBurst = true;

        int shots = burstCount > 0 ? burstCount : Mathf.CeilToInt(1f / timeBetweenBullets);
        for (int i = 0; i < shots; i++)
        {
            if (shootingPoint == null || projectilePrefab == null || playerTarget == null) break;

            // 탄 퍼짐 적용
            Vector3 targetPoint = playerTarget.position + Vector3.up * 1.2f;
            Vector3 idealDir = (targetPoint - shootingPoint.position).normalized;

            // 작은 랜덤 오프셋 회전
            Quaternion spreadRot = Quaternion.Euler(
                Random.Range(-spreadAngle, spreadAngle),
                Random.Range(-spreadAngle, spreadAngle),
                0f);
            Vector3 shootDir = (spreadRot * idealDir).normalized;

            shootingPoint.rotation = Quaternion.LookRotation(shootDir);

            GameObject bullet = Instantiate(projectilePrefab, shootingPoint.position, shootingPoint.rotation);
            var proj = bullet.GetComponent<EnemyProjectile>();
            if (proj != null)
                proj.Init(gameObject, shootDir, projectileSpeed, projectileDamage);

            Collider bulletCol = bullet.GetComponent<Collider>();
            if (bulletCol != null)
            {
                Collider[] enemyCols = GetComponentsInChildren<Collider>();
                foreach (var c in enemyCols) if (c != null) Physics.IgnoreCollision(bulletCol, c, true);
            }

            Destroy(bullet, 5f);

            yield return new WaitForSeconds(timeBetweenBullets);
        }

        isShootingBurst = false;
    }
}
