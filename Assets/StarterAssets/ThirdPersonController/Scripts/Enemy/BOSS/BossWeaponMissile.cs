using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossWeaponMissile : MonoBehaviour
{
    public enum Pattern
    {
        AimedSalvo,     // 플레이어를 조준해 연속 발사(기본)
        SweepLeftRight, // 좌→우→좌로 훑어 쏨(지연 유도)
        Rain,           // 상공에서 비처럼 떨어지고 잠시 후 유도 시작
        Pincer          // 좌우 포드가 동시에 집게처럼 조여옴
    }

    [Header("References")]
    public Transform[] pods;              // 미사일 포드(발사 위치들)
    public GameObject missilePrefab;      // BossHomingMissile 프리팹
    public Transform player;              // 비우면 Character.Instance 사용

    [Header("General")]
    public bool autoStartOnEnable = true;
    public int maxActiveMissiles = 12;    // 동시에 떠 있는 미사일 제한
    public float spawnForwardOffset = 0.5f;

    [Header("Pattern Select")]
    public Pattern currentPattern = Pattern.AimedSalvo;
    public float patternDuration = 6f;    // 한 패턴 유지 시간(끝나면 다음 패턴으로)
    public bool loopPatterns = true;

    [Header("Aimed Salvo")]
    public int aimed_salvos = 2;
    public int aimed_perSalvo = 4;
    public float aimed_betweenShots = 0.25f;
    public float aimed_betweenSalvos = 2.5f;

    [Header("Sweep Left-Right")]
    public int sweep_rows = 3;            // 몇 줄 쏠지
    public int sweep_perRow = 6;          // 한 줄당 발사 수
    public float sweep_betweenShots = 0.18f;
    public float sweep_betweenRows = 1.2f;
    public float sweep_yawAmplitude = 20f; // 좌/우로 얼마나 흔들지(도)
    public float sweep_homingDelay = 0.35f; // 지연 유도(처음엔 직진)

    [Header("Rain")]
    public int rain_count = 10;           // 한 번에 몇 발
    public float rain_height = 12f;       // 플레이어 머리 위 몇 m에서 스폰
    public float rain_spreadRadius = 6f;  // 원형 랜덤 반경
    public float rain_betweenShots = 0.08f;
    public float rain_homingDelay = 0.5f; // 떨어지다가 유도 시작

    [Header("Pincer")]
    public int pincer_salvos = 3;
    public float pincer_betweenShots = 0.22f;
    public float pincer_homingDelay = 0.25f;

    [Header("Enrage (Optional)")]
    public bool enraged = false;
    public float enragedTempoMul = 1.25f; // 분노 시 템포 빨라짐
    public int enragedExtraShots = 1;     // 살보/행마다 +추가

    [Header("Debug")]
    public bool enableLogging = false;

    private bool firing;
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
        if (!firing) StartCoroutine(PatternLoop());
    }

    public void StopFiring()
    {
        firing = false;
        StopAllCoroutines();
    }

    private IEnumerator PatternLoop()
    {
        firing = true;

        while (firing)
        {
            float endTime = Time.time + Mathf.Max(2f, patternDuration);

            switch (currentPattern)
            {
                case Pattern.AimedSalvo:
                    yield return StartCoroutine(Run_AimedSalvo(endTime));
                    break;

                case Pattern.SweepLeftRight:
                    yield return StartCoroutine(Run_SweepLeftRight(endTime));
                    break;

                case Pattern.Rain:
                    yield return StartCoroutine(Run_Rain(endTime));
                    break;

                case Pattern.Pincer:
                    yield return StartCoroutine(Run_Pincer(endTime));
                    break;
            }

            if (!loopPatterns) yield break;

            // 다음 패턴으로 순환
            currentPattern = (Pattern)(((int)currentPattern + 1) % System.Enum.GetValues(typeof(Pattern)).Length);
        }
    }

    // ============= 패턴 구현부 =============

    private IEnumerator Run_AimedSalvo(float endTime)
    {
        while (Time.time < endTime && firing)
        {
            int salvos = aimed_salvos + (enraged ? 1 : 0);
            for (int s = 0; s < salvos && Time.time < endTime && firing; s++)
            {
                int shots = aimed_perSalvo + (enraged ? enragedExtraShots : 0);
                for (int i = 0; i < shots && Time.time < endTime && firing; i++)
                {
                    TryFireAimed(homingDelay: 0f);
                    yield return new WaitForSeconds(aimed_betweenShots / (enraged ? enragedTempoMul : 1f));
                }
                yield return new WaitForSeconds(aimed_betweenSalvos / (enraged ? enragedTempoMul : 1f));
            }
        }
    }

    private IEnumerator Run_SweepLeftRight(float endTime)
    {
        // 좌→우→좌 스윕
        int dir = 1; // 1=우향, -1=좌향
        while (Time.time < endTime && firing)
        {
            for (int row = 0; row < sweep_rows && Time.time < endTime && firing; row++)
            {
                for (int i = 0; i < sweep_perRow + (enraged ? enragedExtraShots : 0); i++)
                {
                    float t = (float)i / Mathf.Max(1, sweep_perRow - 1);
                    float yaw = Mathf.Lerp(-sweep_yawAmplitude, sweep_yawAmplitude, t) * dir;
                    TryFireAimed(yawOffsetDeg: yaw, homingDelay: sweep_homingDelay);
                    yield return new WaitForSeconds(sweep_betweenShots / (enraged ? enragedTempoMul : 1f));
                }
                dir *= -1; // 방향 반전
                yield return new WaitForSeconds(sweep_betweenRows / (enraged ? enragedTempoMul : 1f));
            }
        }
    }

    private IEnumerator Run_Rain(float endTime)
    {
        // 플레이어 머리 위에 랜덤 산포로 떨어지고, 잠시 후 유도 시작
        while (Time.time < endTime && firing)
        {
            Transform t = ResolveTarget();
            Vector3 center = (t != null ? t.position : transform.position);
            for (int i = 0; i < rain_count + (enraged ? enragedExtraShots : 0); i++)
            {
                Vector2 rand = Random.insideUnitCircle * rain_spreadRadius;
                Vector3 spawnPos = new Vector3(center.x + rand.x, center.y + rain_height, center.z + rand.y);
                TryFireAtPosition(spawnPos, Vector3.down, homingDelay: rain_homingDelay);
                yield return new WaitForSeconds(rain_betweenShots / (enraged ? enragedTempoMul : 1f));
            }
            // 잠깐 쉬기
            yield return new WaitForSeconds(1.2f / (enraged ? enragedTempoMul : 1f));
        }
    }

    private IEnumerator Run_Pincer(float endTime)
    {
        // 좌우 포드가 번갈아 동시에 쏘며 집게처럼 조여옴
        while (Time.time < endTime && firing)
        {
            int shots = pincer_salvos + (enraged ? 1 : 0);
            for (int i = 0; i < shots && Time.time < endTime && firing; i++)
            {
                TryFirePincer(homingDelay: pincer_homingDelay);
                yield return new WaitForSeconds(pincer_betweenShots / (enraged ? enragedTempoMul : 1f));
            }
            yield return new WaitForSeconds(1.0f / (enraged ? enragedTempoMul : 1f));
        }
    }

    // ============= 발사 헬퍼들 =============

    private bool CanSpawnMore() => _active.Count < maxActiveMissiles;

    private Transform ResolveTarget()
    {
        if (player != null) return player;
        if (Character.Instance != null) return Character.Instance.transform;
        return null;
    }

    // 기본 조준(플레이어 쪽) 발사
    private void TryFireAimed(float yawOffsetDeg = 0f, float homingDelay = 0f)
    {
        if (!CanSpawnMore() || missilePrefab == null || pods == null || pods.Length == 0) return;

        var pod = pods[_podIndex % pods.Length];
        _podIndex++;

        Transform t = ResolveTarget();
        Vector3 dir = (t != null ? (t.position - pod.position) : transform.forward).normalized;
        if (Mathf.Abs(yawOffsetDeg) > 0.01f)
            dir = Quaternion.Euler(0f, yawOffsetDeg, 0f) * dir;

        Vector3 spawnPos = pod.position + pod.forward * spawnForwardOffset;
        SpawnMissile(spawnPos, Quaternion.LookRotation(dir), t, homingDelay);
    }

    // 특정 위치/방향으로 바로 스폰 (비→유도)
    private void TryFireAtPosition(Vector3 spawnPos, Vector3 direction, float homingDelay = 0f)
    {
        if (!CanSpawnMore() || missilePrefab == null) return;
        SpawnMissile(spawnPos, Quaternion.LookRotation(direction.normalized), ResolveTarget(), homingDelay);
    }

    // 좌/우 동시에 발사
    private void TryFirePincer(float homingDelay = 0f)
    {
        if (pods == null || pods.Length == 0 || missilePrefab == null) return;
        if (pods.Length == 1)
        {
            TryFireAimed(0f, homingDelay);
            return;
        }

        // 좌우 포드 2개 선택(다수면 0,1 사용)
        Transform left = pods[0];
        Transform right = pods[1];

        // 왼쪽
        if (CanSpawnMore())
        {
            Transform t = ResolveTarget();
            Vector3 dir = (t != null ? (t.position - left.position) : left.forward).normalized;
            Vector3 spawnPos = left.position + left.forward * spawnForwardOffset;
            SpawnMissile(spawnPos, Quaternion.LookRotation(dir), t, homingDelay);
        }
        // 오른쪽
        if (CanSpawnMore())
        {
            Transform t = ResolveTarget();
            Vector3 dir = (t != null ? (t.position - right.position) : right.forward).normalized;
            Vector3 spawnPos = right.position + right.forward * spawnForwardOffset;
            SpawnMissile(spawnPos, Quaternion.LookRotation(dir), t, homingDelay);
        }
    }

    private void SpawnMissile(Vector3 pos, Quaternion rot, Transform target, float homingDelay)
    {
        var go = Instantiate(missilePrefab, pos, rot);
        go.name = $"[MISSILE-{Time.frameCount}]_{missilePrefab.name}";
        _active.Add(go);

        var hm = go.GetComponent<BossHomingMissile>();
        if (hm != null)
        {
            hm.owner = transform;
            hm.target = target;
            hm.homingDelay = Mathf.Max(0f, homingDelay);

            // 목표까지 거리로 lifeTime 보정(멀면 오래 날도록)
            if (target != null)
            {
                float dist = Vector3.Distance(pos, target.position);
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
}
