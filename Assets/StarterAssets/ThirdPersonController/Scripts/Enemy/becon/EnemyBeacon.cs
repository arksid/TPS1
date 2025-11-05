using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class EnemyBeacon : MonoBehaviour, IHittable
{
    [Header("생존/체력")]
    [SerializeField] private float maxHP = 200f;
    [SerializeField] private float currentHP;

    [Header("스폰 설정")]
    [Tooltip("이 비콘이 소환할 적 프리팹 목록(지상형 NavMeshAgent 권장)")]
    public GameObject[] enemyPrefabs;

    [Tooltip("스폰 간격(초)")]
    public float spawnInterval = 5f;

    [Tooltip("비콘 주변 스폰 반경")]
    public float spawnRadius = 8f;

    [Tooltip("한 번에 살아있을 수 있는 최대 적 수")]
    public int maxAlive = 6;

    [Tooltip("전체 소환 상한(0이면 무제한)")]
    public int totalSpawnCap = 0;

    [Tooltip("플레이어가 너무 멀면 일시중지하는 거리(0이면 무시)")]
    public float pauseBeyondDistance = 60f;

    [Header("VFX/SFX")]
    public GameObject spawnEffectPrefab;
    public GameObject breakEffectPrefab;
    public AudioClip spawnSfx;
    public AudioClip breakSfx;

    [Header("기타 옵션")]
    [Tooltip("NavMesh 영역에서만 스폰 시도")]
    public bool ensureNavMeshSpawn = true;

    [Tooltip("비콘을 표시할 아웃라인(있으면 On/Off)")]
    public Outline outlineWhenActive;

    // 내부 상태
    private int aliveCount = 0;
    private int totalSpawned = 0;
    private Transform player;
    private bool isRunning = false;
    private AudioSource _audio;

    void Awake()
    {
        currentHP = maxHP;
        _audio = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        // 플레이어 찾기
        if (Character.Instance != null) player = Character.Instance.transform;
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) player = go.transform;
        }

        // 아웃라인 표시(선택)
        if (outlineWhenActive != null) outlineWhenActive.enabled = true;

        // 스폰 루프 시작
        if (!isRunning) StartCoroutine(CoSpawnLoop());
    }

    void OnDisable()
    {
        if (outlineWhenActive != null) outlineWhenActive.enabled = false;
        isRunning = false;
        StopAllCoroutines();
    }

    // ✅ 인터페이스 시그니처 정확히 구현
    public void OnHit(int damage)
    {
        ApplyDamage(damage);
    }

    // (선택) 다른 시스템에서 float/히트포인트 정보를 주고 싶을 때 호출할 수 있는 오버로드
    public void OnHit(float damage, Vector3 hitPoint, Vector3 hitNormal, GameObject source)
    {
        ApplyDamage(Mathf.RoundToInt(damage));
    }

    private void ApplyDamage(int amount)
    {
        if (amount <= 0) return;

        currentHP -= amount;
        if (currentHP < 0) currentHP = 0;

        Debug.Log($"[EnemyBeacon] 피격! -{amount}, HP {currentHP}/{maxHP}");

        // (선택) HP UI 표시
        if (CanvasManager.singleton != null)
            CanvasManager.singleton.ShowDamage(amount);

        if (currentHP <= 0)
        {
            Break();
        }
    }

    private void Break()
    {
        if (breakEffectPrefab != null)
            Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
        if (_audio && breakSfx) _audio.PlayOneShot(breakSfx);

        if (outlineWhenActive != null) outlineWhenActive.enabled = false;
        StopAllCoroutines();

        Destroy(gameObject, 0.05f);
    }

    private IEnumerator CoSpawnLoop()
    {
        isRunning = true;

        var wait = new WaitForSeconds(spawnInterval);
        while (isRunning)
        {
            // 플레이어 거리 체크(선택)
            if (pauseBeyondDistance > 0f && player != null)
            {
                float d = Vector3.Distance(transform.position, player.position);
                if (d > pauseBeyondDistance)
                {
                    yield return wait;
                    continue;
                }
            }

            // 현재 생존 수 제한
            if (aliveCount < maxAlive)
            {
                // 전체 상한 체크
                if (totalSpawnCap <= 0 || totalSpawned < totalSpawnCap)
                {
                    TrySpawnEnemy();
                }
            }

            yield return wait;
        }
    }

    private void TrySpawnEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        // 랜덤 적 선택
        var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        if (prefab == null) return;

        // 비콘 주변 랜덤 위치
        Vector2 r = Random.insideUnitCircle * spawnRadius;
        Vector3 pos = transform.position + new Vector3(r.x, 0f, r.y);

        // NavMesh 보정
        if (ensureNavMeshSpawn)
        {
            if (NavMesh.SamplePosition(pos, out var hit, 5f, NavMesh.AllAreas))
                pos = hit.position;
        }

        // 스폰
        GameObject enemy = Instantiate(prefab, pos, Quaternion.identity);

        // NavMeshAgent 워프(경계 보정)
        var ag = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (ag && NavMesh.SamplePosition(enemy.transform.position, out var nHit, 5f, NavMesh.AllAreas))
            ag.Warp(nHit.position);

        // 비콘 카운트 연동
        var byBeacon = enemy.GetComponent<EnemySpawnedByBeacon>();
        if (byBeacon == null) byBeacon = enemy.AddComponent<EnemySpawnedByBeacon>();
        byBeacon.owner = this;

        aliveCount++;
        totalSpawned++;

        // 효과/소리
        if (spawnEffectPrefab != null)
            Instantiate(spawnEffectPrefab, pos, Quaternion.identity);
        if (_audio && spawnSfx) _audio.PlayOneShot(spawnSfx);
    }

    public void NotifyChildDied()
    {
        aliveCount = Mathf.Max(0, aliveCount - 1);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
