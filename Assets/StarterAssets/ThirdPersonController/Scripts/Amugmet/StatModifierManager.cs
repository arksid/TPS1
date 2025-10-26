using UnityEngine;

[DefaultExecutionOrder(-100)] // 가장 먼저 초기화 → 다른 시스템에서 안전하게 Instance 참조 가능
[DisallowMultipleComponent]
public class StatModifierManager : MonoBehaviour
{
    public static StatModifierManager Instance { get; private set; }

    // === 누적 배율(기본 1.0) ===
    public float DamageMultiplier { get; private set; } = 1f;
    public float FireRateMultiplier { get; private set; } = 1f;
    public float MoveSpeedMultiplier { get; private set; } = 1f;

    // === 누적 가산 ===
    public float HealOnKill { get; private set; } = 0f;          // 처치 시 회복량
    public float UltimateOnHitCharge { get; private set; } = 0f; // 명중 시 궁극기 게이지 가산(프로젝트 규약에 맞게 %/고정치)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ─────────────────────────────────────────────────────────────
    // 배율형 변경 (누적, 1.0 기준)
    // ─────────────────────────────────────────────────────────────
    public void AddDamageMultiplier(float value)
    {
        DamageMultiplier += value;
        if (DamageMultiplier < 0.1f) DamageMultiplier = 0.1f;
        Debug.Log($"[StatMod] DamageMultiplier = {DamageMultiplier:0.00}x");
    }

    public void AddFireRateMultiplier(float value)
    {
        FireRateMultiplier += value;
        if (FireRateMultiplier < 0.1f) FireRateMultiplier = 0.1f;
        Debug.Log($"[StatMod] FireRateMultiplier = {FireRateMultiplier:0.00}x");
    }

    public void AddMoveSpeedMultiplier(float value)
    {
        MoveSpeedMultiplier += value;
        if (MoveSpeedMultiplier < 0.1f) MoveSpeedMultiplier = 0.1f;
        Debug.Log($"[StatMod] MoveSpeedMultiplier = {MoveSpeedMultiplier:0.00}x");

        // 필요 시 즉시 캐릭터 이동속도에 반영하고 싶다면 Character 쪽에서 이 배율을 참조해 곱해 주세요.
    }

    // ─────────────────────────────────────────────────────────────
    // 가산형(체력/실드/크리티컬/힐/궁극기 등)
    // ─────────────────────────────────────────────────────────────
    /// <summary>치명타 확률을 퍼센트포인트(예: +15f → +15%p)로 가산</summary>
    public void AddCriticalChance(float percentPoints)
    {
        var ch = Character.Instance ?? FindObjectOfType<Character>();
        if (ch != null)
        {
            ch.CriticalChance += percentPoints;
            Debug.Log($"[StatMod] Crit +{percentPoints}%p → {ch.CriticalChance}%");
        }
        else
        {
            Debug.LogWarning("[StatMod] Character 없음: CriticalChance 가산 스킵");
        }
    }

    public void AddHealth(int amount)
    {
        var ch = Character.Instance ?? FindObjectOfType<Character>();
        if (ch != null)
        {
            ch.Heal(amount);
            Debug.Log($"[StatMod] Heal +{amount}");
        }
        else
        {
            Debug.LogWarning("[StatMod] Character 없음: Heal 스킵");
        }
    }

    public void AddShield(int amount)
    {
        var ch = Character.Instance ?? FindObjectOfType<Character>();
        if (ch != null)
        {
            ch.RestoreShield(amount);
            Debug.Log($"[StatMod] Shield +{amount}");
        }
        else
        {
            Debug.LogWarning("[StatMod] Character 없음: Shield 스킵");
        }
    }

    public void AddHealOnKill(float amount)
    {
        HealOnKill += amount;

        // 프로젝트에 onKillHealAmount 같은 변수가 있다면 같이 누적
        var ch = Character.Instance ?? FindObjectOfType<Character>();
        if (ch != null)
        {
            ch.onKillHealAmount += amount; // 없다면 이 줄은 지워도 됨
        }

        Debug.Log($"[StatMod] HealOnKill += {amount} (total {HealOnKill})");
    }

    public void AddUltimateCharge(float amount)
    {
        UltimateOnHitCharge += amount;
        Debug.Log($"[StatMod] UltimateOnHitCharge += {amount} (total {UltimateOnHitCharge})");

        // 실제 충전은 '명중 시' 이벤트에서 처리: OnPlayerHitEnemy()에서 UltimateOnHitCharge를 사용
    }

    // ─────────────────────────────────────────────────────────────
    // 이벤트 훅 (QuickTester/HUD와 연동)
    // ─────────────────────────────────────────────────────────────
    /// <summary>적 처치 시 호출(HealOnKill 검증용). EnemyController 등에서 호출해도 됨.</summary>
    public void OnEnemyKilled()
    {
        if (HealOnKill <= 0f) return;

        var ch = Character.Instance ?? FindObjectOfType<Character>();
        if (ch != null)
        {
            ch.Heal(Mathf.RoundToInt(HealOnKill));
            Debug.Log($"[StatMod] OnEnemyKilled → Heal {HealOnKill}");
        }
    }

    /// <summary>플레이어가 적을 명중했을 때 호출(UltimateOnHitCharge 검증용)</summary>
    public void OnPlayerHitEnemy()
    {
        if (UltimateOnHitCharge <= 0f) return;

        var ult = FindObjectOfType<UltimateSkill>();
        if (ult != null)
        {
            ult.AddGauge(UltimateOnHitCharge);
            Debug.Log($"[StatMod] OnPlayerHitEnemy → Ult +{UltimateOnHitCharge}");
        }
        else
        {
            Debug.LogWarning("[StatMod] UltimateSkill 없음: Ult 충전 스킵");
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 리셋 (디버그용)
    // ─────────────────────────────────────────────────────────────
    public void ResetAll()
    {
        DamageMultiplier = 1f;
        FireRateMultiplier = 1f;
        MoveSpeedMultiplier = 1f;
        HealOnKill = 0f;
        UltimateOnHitCharge = 0f;

        // 캐릭터 측 수치도 초기화할지 여부는 프로젝트 정책에 따름
        var ch = Character.Instance ?? FindObjectOfType<Character>();
        if (ch != null)
        {
            // 예: 크리확/이속 등도 기본값으로 되돌리고 싶다면 여기서 처리
            // ch.CriticalChance = ch.baseCriticalChance;
        }

        Debug.Log("[StatMod] ResetAll");
    }
}
