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

    // === [추가] 반동/탄 관련 전역 스탯 ===
    // 반동 배율(1.0이 기본 / 0.8이면 반동 20% 감소)
    public float RecoilMultiplier { get; private set; } = 1f;

    // 장탄수(탄창 용량) 보너스. +5면 30발 → 35발
    public int MagazineSizeBonus { get; private set; } = 0;


    // === 이동 중 반동 감소(스텝 앤 건) ===
    public bool StepAndGunEnabled { get; private set; } = false;
    public float StepAndGunRecoilReduce { get; private set; } = 0f; // 0.2 = 20% 감소
    public float StepAndGunMoveThreshold { get; private set; } = 0.1f; // 이동 판단 임계값(속도)

    // === 빠른 장전(리로드 속도 배율) ===
    public float ReloadSpeedMultiplier { get; private set; } = 1f; // 1.3 = 30% 빠름
    public float QuickReloadBuffRemain { get; private set; } = 0f;  // 남은 시간(초)

    // === 관통 횟수 보너스 ===
    public int ProjectilePenetrationBonus { get; private set; } = 0;

    // === 과열탄(연속 명중 스택) ===
    public int OverheatHitNeed { get; private set; } = 4;
    public float OverheatBuffValue { get; private set; } = 0.25f; // 25% 데미지
    public float OverheatBuffDuration { get; private set; } = 2f;

    // === 기어 올라가기(리로드 직후 버프) ===
    public float GearUpBuffValue { get; private set; } = 0.2f;
    public float GearUpBuffDuration { get; private set; } = 3f;

    // === 찢어발기기(동일 적 히트 트래킹) ===
    public int RendHitNeed { get; private set; } = 3;
    public float RendWindow { get; private set; } = 1f;
    public float RendBonus { get; private set; } = 0.2f;
    public float RendDuration { get; private set; } = 5f;


    // (선택) 예비탄 보너스도 원하면 사용: public int ReserveAmmoBonus { get; private set; } = 0;
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
    // === [ADD] AugmentSystem 호환용 얇은 래퍼들 ===

    // IronSkin용: 실드 "최대치"를 증감
    public void AddMaxShield(int delta)
    {
        var ch = Character.Instance ?? FindObjectOfType<Character>();
        if (ch != null)
        {
            int oldMax = ch.MaxShield;
            ch.MaxShield = Mathf.Max(0, ch.MaxShield + delta);
            // 현재 실드가 최대치보다 크면 잘라주기
            ch.Shield = Mathf.Min(ch.Shield, ch.MaxShield);
            Debug.Log($"[StatMod] MaxShield {oldMax} → {ch.MaxShield} (Δ {delta})");
        }
        else
        {
            Debug.LogWarning("[StatMod] Character 없음: AddMaxShield 스킵");
        }
    }

    // Survivor용: 처치 시 회복량(누적) — 기존 AddHealOnKill과 동일 동작
    public void AddOnKillHeal(int amount) { AddHealOnKill(amount); }
    public void AddOnKillHeal(float amount) { AddHealOnKill(amount); }
    public void AddRecoilReduction(float percent)
    {
        // percent: 0.2f → 반동 20% 감소
        // RecoilMultiplier = 1 - percent 누적 (하한 0.2)
        float newMul = RecoilMultiplier * Mathf.Clamp01(1f - percent);
        RecoilMultiplier = Mathf.Max(newMul, 0.2f);
        Debug.Log($"[StatMod] RecoilMultiplier = {RecoilMultiplier:0.00} (↓{percent * 100f:0}% 적용)");
    }

    public void AddMagazineSizeBonus(int amount)
    {
        MagazineSizeBonus += amount;
        if (MagazineSizeBonus < 0) MagazineSizeBonus = 0;
        Debug.Log($"[StatMod] MagazineSizeBonus = +{MagazineSizeBonus}");
    }
    // Step&Gun 토글/값
    public void EnableStepAndGun(float reducePercent, float moveThreshold = 0.1f)
    {
        StepAndGunEnabled = true;
        StepAndGunRecoilReduce = Mathf.Clamp01(reducePercent);
        StepAndGunMoveThreshold = Mathf.Max(0.01f, moveThreshold);
    }
    public void DisableStepAndGun()
    {
        StepAndGunEnabled = false;
        StepAndGunRecoilReduce = 0f;
    }

    // QuickReload: 5초 내 다음 리로드 속도 배율(누적 X, 시간 연장 O)
    public void GrantQuickReload(float speedMul, float duration)
    {
        ReloadSpeedMultiplier = Mathf.Max(1f, speedMul);
        QuickReloadBuffRemain = Mathf.Max(QuickReloadBuffRemain, duration);
    }
    public void TickQuickReload(float dt)
    {
        if (QuickReloadBuffRemain > 0f)
        {
            QuickReloadBuffRemain -= dt;
            if (QuickReloadBuffRemain <= 0f)
            {
                QuickReloadBuffRemain = 0f;
                ReloadSpeedMultiplier = 1f;
            }
        }
    }

    // Penetrator
    public void AddPenetrationBonus(int add)
    {
        ProjectilePenetrationBonus = Mathf.Max(0, ProjectilePenetrationBonus + add);
    }

    // GearUp / Overheat / Rend 값은 Character가 시간형으로 관리 (StatMod는 값 제공만)

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
        RecoilMultiplier = 1f;
        MagazineSizeBonus = 0;
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
