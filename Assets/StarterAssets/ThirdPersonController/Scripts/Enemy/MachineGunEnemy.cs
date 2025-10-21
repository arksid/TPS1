using System.Collections;
using UnityEngine;

public class MachineGunEnemy : EnemyController
{
    [Header("기관총 설정")]
    public int burstCount = 5;
    public float timeBetweenBullets = 0.08f;
    public float spreadAngle = 6f;

    private bool isShootingBurst = false;

    protected override void Start()
    {
        base.Start();
    }

    protected override void Shoot()
    {
        if (!isShootingBurst)
            StartCoroutine(BurstRoutine());
    }

    private IEnumerator BurstRoutine()
    {
        isShootingBurst = true;

        int shots = burstCount > 0 ? burstCount : Mathf.CeilToInt(1f / timeBetweenBullets);
        for (int i = 0; i < shots; i++)
        {
            if (shootingPoint == null || projectilePrefab == null || playerTarget == null)
                break;

            // 총알 퍼짐 처리
            Vector3 targetPoint = playerTarget.position + Vector3.up * 1.2f;
            Vector3 idealDir = (targetPoint - shootingPoint.position).normalized;
            Quaternion spreadRot = Quaternion.Euler(
                Random.Range(-spreadAngle, spreadAngle),
                Random.Range(-spreadAngle, spreadAngle),
                0f
            );
            Vector3 shootDir = (spreadRot * idealDir).normalized;

            shootingPoint.rotation = Quaternion.LookRotation(shootDir);

            GameObject bullet = Instantiate(projectilePrefab, shootingPoint.position, shootingPoint.rotation);
            var proj = bullet.GetComponent<EnemyProjectile>();
            if (proj != null)
                proj.Init(gameObject, shootDir, projectileSpeed, projectileDamage);

            // 적 본인과 총알 충돌 방지
            Collider bulletCol = bullet.GetComponent<Collider>();
            if (bulletCol != null)
            {
                Collider[] enemyCols = GetComponentsInChildren<Collider>();
                foreach (var c in enemyCols)
                    if (c != null) Physics.IgnoreCollision(bulletCol, c, true);
            }

            Destroy(bullet, 5f);

            yield return new WaitForSeconds(timeBetweenBullets);
        }

        isShootingBurst = false;
    }
    protected override void Die()
    {
        base.Die();
        var dropSystem = GetComponent<EnemyDropSystem>();
        if (dropSystem != null) dropSystem.TryDropItemByWeight();
    }

}
