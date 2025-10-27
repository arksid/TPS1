using UnityEngine;
using System.Collections;

public class BossWeaponMinigun : MonoBehaviour, ISlowable
{
    [Header("References")]
    public Transform muzzle;              // 총구(메쉬 밖으로 배치)
    public GameObject bulletPrefab;       // PF_BossBullet (BossBullet 스크립트 포함)
    public Transform player;              // 비우면 Character.Instance 사용

    [Header("Fire Settings")]
    public float bulletSpeed = 30f;
    public float rpm = 600f;
    public int burstCount = 20;
    public float spreadAngle = 2.0f;

    [Header("Enraged Tuning (Optional)")]
    public float enragedRpmMul = 1.35f;
    public int enragedExtraShots = 10;

    [Header("Behavior")]
    public bool autoStartOnEnable = true; // 켜지면 자동 발사

    [Header("Debug")]
    public bool enableLogging = false;

    private bool firing;
    private float localTimeScale = 1f;    // 1.0=정상, 0.5=슬로우
    private bool enraged = false;

    void OnEnable()
    {
        if (autoStartOnEnable) StartFiring();
    }

    void OnDisable()
    {
        StopFiring();
    }

    public void StartFiring()
    {
        if (!firing) StartCoroutine(FireLoop());
        if (enableLogging) Debug.Log("<Minigun> StartFiring()");
    }

    public void StopFiring()
    {
        firing = false;
        if (enableLogging) Debug.Log("<Minigun> StopFiring()");
    }

    public void SetEnraged(bool on)
    {
        enraged = on;
        if (enableLogging) Debug.Log($"<Minigun> SetEnraged({on})");
    }

    public void SetLocalTimeScale(float scale)
    {
        localTimeScale = Mathf.Clamp(scale, 0.05f, 5f);
        if (enableLogging) Debug.Log($"<Minigun> LocalTimeScale={localTimeScale:0.00}");
    }

    private IEnumerator FireLoop()
    {
        firing = true;
        while (firing)
        {
            float rps = (rpm * (enraged ? enragedRpmMul : 1f)) / 60f;   // 초당 발사 수
            float interval = 1f / Mathf.Max(1f, rps);
            int count = burstCount + (enraged ? enragedExtraShots : 0);

            if (enableLogging)
                Debug.Log($"<Minigun> Burst start | rps={rps:F2}, interval={interval:F3}, count={count}");

            for (int i = 0; i < count; i++)
            {
                FireOne();
                yield return new WaitForSeconds(interval / Mathf.Max(0.01f, localTimeScale)); // 슬로우 고려
                if (!firing) break;
            }

            // 짧은 쿨타임
            yield return new WaitForSeconds(0.6f / Mathf.Max(0.01f, localTimeScale));
        }
    }

    private void FireOne()
    {
        if (muzzle == null || bulletPrefab == null)
        {
            if (enableLogging) Debug.LogWarning("<Minigun> muzzle/bulletPrefab 누락");
            return;
        }

        Transform t = ResolveTarget();
        Vector3 baseDir = (t != null ? (t.position - muzzle.position).normalized : muzzle.forward);

        // 간단 퍼짐
        float yaw = Random.Range(-spreadAngle, spreadAngle);
        float pitch = Random.Range(-spreadAngle, spreadAngle);
        baseDir = Quaternion.Euler(pitch, yaw, 0f) * baseDir;

        var go = Instantiate(bulletPrefab, muzzle.position, Quaternion.LookRotation(baseDir));
        var bb = go.GetComponent<BossBullet>();
        if (bb != null)
        {
            bb.speed = bulletSpeed;
            bb.enableLogging = enableLogging;

            // 목표까지 거리 기반으로 lifeTime/최대거리 자동 보정 (여유분 포함)
            if (t != null)
            {
                float dist = Vector3.Distance(muzzle.position, t.position);
                float sec = Mathf.Max(2f, dist / Mathf.Max(1f, bulletSpeed) + 1.0f);  // 비행시간+여유1s
                bb.lifeTime = Mathf.Max(bb.lifeTime, sec);
                bb.maxTravelDistance = Mathf.Max(bb.maxTravelDistance, dist + 10f);
            }

            // 발사(머리쪽 조준 + 소유자충돌무시 + 안전스폰은 BossBullet 내부)
            bb.FireAtTarget(t != null ? t : muzzle, transform);
        }
        else
        {
            // 백업(권장 X): BossBullet이 없을 때 RB로만 가속
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = baseDir * bulletSpeed;
            if (enableLogging) Debug.LogWarning("<Minigun> BossBullet 스크립트 없음 – RB 가속 사용");
        }

        go.tag = "EnemyProjectile";

        if (enableLogging)
            Debug.Log($"<Minigun> Shot | pos={muzzle.position} dir={baseDir} speed={bulletSpeed}");
    }

    private Transform ResolveTarget()
    {
        if (player != null) return player;
        if (Character.Instance != null) return Character.Instance.transform; // 프로젝트 전역 플레이어
        return null;
    }
}
