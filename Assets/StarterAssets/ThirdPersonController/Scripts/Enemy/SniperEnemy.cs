using UnityEngine;
using System.Collections;

public class SniperEnemy : EnemyController
{
    [Header("저격 설정")]
    public float chargeTime = 1.0f;       // 조준(장전) 시간
    public float zoomFOV = 30f;           // (선택) 조준 시 카메라 효과용
    public float followWhileCharging = 0.5f; // 조준 중 약간 움직일지

    private bool isCharging = false;

    protected override void Start()
    {
        base.Start();
        // 저격병은 기본적으로 긴 사거리
        // shootRange는 인스펙터에서 충분히 크게 잡으세요 (예: 60~100)
    }

    protected override void Shoot()
    {
        if (!isCharging)
            StartCoroutine(ChargeAndShoot());
    }

    private IEnumerator ChargeAndShoot()
    {
        if (shootingPoint == null || projectilePrefab == null || playerTarget == null) yield break;

        isCharging = true;

        // (선택) 애니메이션 트리거: 조준 시작
        if (animator != null) animator.SetBool("isAiming", true);

        // 조준 시간 동안 천천히 플레이어를 추적(또는 고정)
        float t = 0f;
        while (t < chargeTime)
        {
            t += Time.deltaTime;
            if (agent != null && followWhileCharging > 0f)
            {
                // 미세 이동: 플레이어를 계속 바라보되 속도는 낮춤
                agent.SetDestination(Vector3.Lerp(transform.position, playerTarget.position, followWhileCharging * Time.deltaTime));
            }
            yield return null;
        }

        // 조준 끝나면 한발 발사 (정확도 매우 높음)
        Vector3 targetPoint = playerTarget.position + Vector3.up * 1.2f;
        Vector3 shootDir = (targetPoint - shootingPoint.position).normalized;
        shootingPoint.rotation = Quaternion.LookRotation(shootDir);

        // (선택) 강한 데미지, 빠른 속도
        float sniperSpeed = projectileSpeed * 1.5f;
        float sniperDamage = projectileDamage * 4f;

        GameObject bullet = Instantiate(projectilePrefab, shootingPoint.position, shootingPoint.rotation);
        var proj = bullet.GetComponent<EnemyProjectile>();
        if (proj != null)
            proj.Init(gameObject, shootDir, sniperSpeed, sniperDamage);

        Collider bulletCol = bullet.GetComponent<Collider>();
        if (bulletCol != null)
        {
            Collider[] enemyCols = GetComponentsInChildren<Collider>();
            foreach (var c in enemyCols) if (c != null) Physics.IgnoreCollision(bulletCol, c, true);
        }

        Destroy(bullet, 8f); // 장거리라 수명 길게

        // (선택) 애니메이터 복원
        if (animator != null) animator.SetBool("isAiming", false);

        isCharging = false;
    }
}
