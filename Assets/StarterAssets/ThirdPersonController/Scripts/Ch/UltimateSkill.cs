using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UltimateSkill : MonoBehaviour
{
    [Header("궁극기 설정")]
    public float ultimateDuration = 20f;      // 지속 시간(초)
    [Range(0.05f, 1f)] public float slowFactor = 0.2f;  // 느려질 비율(적/적탄)

    [Header("궁극기 강화 설정")]
    public float damageMultiplier = 2.0f;     // 데미지 2배
    public float fireRateMultiplier = 0.5f;   // 발사 간격 절반(=발사속도 2배)

    [Header("게이지 설정")]
    [SerializeField] private float maxGauge = 100f;
    [SerializeField] private float currentGauge = 0f;
    [SerializeField] private float gaugePerHit = 5f;

    [Header("참조")]
    [Tooltip("플레이어 루트 오브젝트(비우면 태그 Player에서 자동 탐색)")]
    public GameObject playerRoot;

    // 런타임 상태
    private bool isActive = false;
    private readonly List<ISlowable> slowed = new List<ISlowable>();
    private Transform _playerRootTr;

    // 외부에서 참조할 전역 상태
    public static bool IsUltimateActive { get; private set; } = false;
    public static float CurrentSlowFactor { get; private set; } = 1f;
    public static float CurrentDamageMultiplier { get; private set; } = 1f;
    public static float CurrentFireRateMultiplier { get; private set; } = 1f;

    public float GaugePerHit => gaugePerHit;

    private void Awake()
    {
        if (playerRoot == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerRoot = p;
        }
        _playerRootTr = playerRoot != null ? playerRoot.transform : null;
    }

    private void Update()
    {
        // Q + 게이지 충족 시 발동
        if (Input.GetKeyDown(KeyCode.Q) && !isActive && currentGauge >= maxGauge)
        {
            ActivateUltimate();
        }
    }

    private void ActivateUltimate()
    {
        isActive = true;
        IsUltimateActive = true;

        // 전역 멀티플라이어 세팅
        CurrentSlowFactor = slowFactor;
        CurrentDamageMultiplier = damageMultiplier;
        CurrentFireRateMultiplier = fireRateMultiplier;

        // 게이지 소모 및 UI 반영
        currentGauge = 0f;
        if (CanvasManager.singleton != null)
            CanvasManager.singleton.UpdateUltimateGauge(0f);

        // 적/적탄만 슬로우
        slowed.Clear();
        var all = FindObjectsOfType<MonoBehaviour>(true);
        foreach (var mb in all)
        {
            if (mb is ISlowable s)
            {
                if (ShouldBeSlowed(mb))
                {
                    s.SetLocalTimeScale(slowFactor);
                    slowed.Add(s);
                }
            }
        }

        Debug.Log("[ULT] 발동: 적/적탄 슬로우 + 무한탄창 + 발사속도x2 + 데미지x2");
        StartCoroutine(EndUltimate());
    }

    private IEnumerator EndUltimate()
    {
        yield return new WaitForSeconds(ultimateDuration);

        // 슬로우 해제
        foreach (var s in slowed)
        {
            if (s != null) s.SetLocalTimeScale(1f);
        }
        slowed.Clear();

        // 전역 멀티플라이어 원복
        isActive = false;
        IsUltimateActive = false;
        CurrentSlowFactor = 1f;
        CurrentDamageMultiplier = 1f;
        CurrentFireRateMultiplier = 1f;

        Debug.Log("[ULT] 종료");
    }

    // 플레이어가 소유/소속한 오브젝트는 제외하고 슬로우
    private bool ShouldBeSlowed(MonoBehaviour mb)
    {
        if (_playerRootTr == null) return true; // 플레이어 참조 없으면 전부 슬로우

        // 1) 플레이어 트랜스폼 하위이면 제외
        var t = mb.transform;
        if (t.IsChildOf(_playerRootTr)) return false;

        // 2) Projectile인 경우: shooter가 Player면 제외, 그 외(적탄 등)는 포함
        var proj = mb as Projectile;
        if (proj != null)
        {
            if (proj.shooter != null && proj.shooter.CompareTag("Player"))
                return false; // 플레이어 탄은 제외
            return true;       // 그 외(적탄)는 포함
        }

        // 3) Enemy 태그면 포함
        if (mb.gameObject.CompareTag("Enemy")) return true;

        // 4) EnemyProjectile 태그면 포함(있다면)
        if (mb.gameObject.CompareTag("EnemyProjectile")) return true;

        // 그 외는 기본적으로 제외 (필요시 레이어/태그로 추가)
        return false;
    }

    public void AddGauge(float amount)
    {
        currentGauge = Mathf.Clamp(currentGauge + amount, 0f, maxGauge);
        if (CanvasManager.singleton != null)
            CanvasManager.singleton.UpdateUltimateGauge(currentGauge / maxGauge);

        // 디버그
        // Debug.Log($"[ULT] 게이지 {currentGauge}/{maxGauge}");
    }
}

// 느려질 수 있는 대상이 구현
public interface ISlowable
{
    void SetLocalTimeScale(float scale);
}
