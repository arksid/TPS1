using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossWeaponMissile : MonoBehaviour
{
    [Header("References")]
    public Transform[] pods;              // 미사일 포드(여러 발사 위치)
    public GameObject missilePrefab;      // BossHomingMissile 프리팹
    public Transform player;              // 비우면 Character.Instance 사용

    [Header("Firing Pattern")]
    public bool autoStartOnEnable = true; // 켜지면 자동 발사
    public int salvosPerCycle = 2;        // 사이클당 살보 수
    public int missilesPerSalvo = 4;      // 살보당 발사 수(포드 순환)
    public float intervalBetweenShots = 0.25f;  // 동일 살보 내 간격
    public float intervalBetweenSalvos = 2.5f;  // 살보 간 간격
    public float cycleCooldown = 4.0f;          // 사이클 간 쿨다운

    [Header("Limits")]
    public int maxActiveMissiles = 12;    // 동시에 존재 가능한 미사일 수
    public float spawnForwardOffset = 0.6f; // 스폰 전방 오프셋(자기 충돌 방지)

    [Header("Enrage (Optional)")]
    public bool enraged = false;
    public float enragedShotMul = 1.25f;       // 분노 시 템포↑
    public int enragedExtraPerSalvo = 2;       // 분노 시 살보당 추가 발사

    [Header("Debug")]
    public bool enableLogging = false;

    // 내부 상태
    private bool firing;
    private float localTimeScale = 1f;         // 1.0=정상, 0.5=슬로우
    private readonly List<GameObject> _active = new List<GameObject>();
    private int _podIndex = 0;

    void OnEnable()
    {
        if (autoStartOnEnable) StartFiring();
    }

    void OnDisable()
    {
        StopFiring();
        _active.Clear();
    }

    public void StartFiring()
    {
        if (!firing) StartCoroutine(FireLoop());
        if (enableLogging) Debug.Log("<Missile> StartFiring()");
    }

    public void StopFiring()
    {
        firing = false;
        if (enableLogging) Debug.Log("<Missile> StopFiring()");
    }

    public void SetEnraged(bool on)
    {
        enraged = on;
        if (enableLogging) Debug.Log($"<Missile> SetEnraged({on})");
    }

    public void SetLocalTimeScale(float scale)
    {
        localTimeScale = Mathf.Clamp(scale, 0.05f, 5f);
        if (enableLogging) Debug.Log($"<Missile> LocalTimeScale={localTimeScale:0.00}");
    }

    private IEnumerator FireLoop()
    {
        firing = true;
        while (firing)
        {
            if (!IsReady()) { yield return null; continue; }

            int salvos = Mathf.Max(0, salvosPerCycle);
            for (int s = 0; s < salvos && firing; s++)
            {
                int shots = Mathf.Max(0, missilesPerSalvo + (enraged ? enragedExtraPerSalvo : 0));
                float shotGap = intervalBetweenShots / (enraged ? Mathf.Max(0.01f, enragedShotMul) : 1f);

                for (int i = 0; i < shots && firing; i++)
                {
                    // 동시 활성 수 제한
                    if (_active.Count >= maxActiveMissiles)
                    {
                        if (enableLogging) Debug.Log("<Missile> Max active reached, waiting…");
                        yield return new WaitForSeconds(0.2f / Mathf.Max(0.01f, localTimeScale));
                        i--; // 이번 샷 재시도
                        continue;
                    }

                    FireOne();
                    yield return new WaitForSeconds(shotGap / Mathf.Max(0.01f, localTimeScale));
                }

                yield return new WaitForSeconds(intervalBetweenSalvos / Mathf.Max(0.01f, localTimeScale));
            }

            yield return new WaitForSeconds(cycleCooldown / Mathf.Max(0.01f, localTimeScale));
        }
    }

    private bool IsReady()
    {
        if (missilePrefab == null) { if (enableLogging) Debug.LogWarning("<Missile> missilePrefab 미할당"); return false; }
        if (pods == null || pods.Length == 0) { if (enableLogging) Debug.LogWarning("<Missile> pods 비어있음"); return false; }
        return true;
    }

    private void FireOne()
    {
        if (!IsReady()) return;

        var pod = pods[_podIndex % pods.Length];
        _podIndex++;

        Transform target = ResolveTarget();
        var spawnPos = pod.position + pod.forward * spawnForwardOffset;

        var go = Instantiate(missilePrefab, spawnPos, pod.rotation);
        go.name = $"[MISSILE-{Time.frameCount}]_{missilePrefab.name}";
        _active.Add(go);

        var hm = go.GetComponent<BossHomingMissile>();
        if (hm != null)
        {
            hm.owner = transform;
            hm.target = target;

            // 목표까지 거리로 수명 보정(멀면 더 오래 날도록)
            if (target != null)
            {
                float dist = Vector3.Distance(spawnPos, target.position);
                float sec = Mathf.Max(3f, dist / Mathf.Max(1f, hm.maxSpeed * 0.8f) + 2.0f);
                hm.lifeTime = Mathf.Max(hm.lifeTime, sec);
            }

            hm.onDestroyed += () => { _active.Remove(go); };
            hm.Launch(transform, target);
        }
        else
        {
            if (enableLogging) Debug.LogWarning("<Missile> BossHomingMissile 스크립트가 프리팹에 없습니다.");
        }
    }

    private Transform ResolveTarget()
    {
        if (player != null) return player;
        if (Character.Instance != null) return Character.Instance.transform;
        return null;
    }
}
