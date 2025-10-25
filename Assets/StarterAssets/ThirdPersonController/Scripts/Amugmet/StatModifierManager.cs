using UnityEngine;

public class StatModifierManager : MonoBehaviour
{
    public static StatModifierManager Instance;

    // === 기본 스탯 관련 ===
    public float DamageMultiplier { get; private set; } = 1f;
    public float FireRateMultiplier { get; private set; } = 1f;
    public float MoveSpeedMultiplier { get; private set; } = 1f;

    // === 특수 효과 관련 ===
    private bool nextShotCritGuaranteed = false;
    private bool nextShotExplosive = false;
    private float nextShotDamageMultiplier = 1f;

    // === 체력 / 실드 / 궁극기 관련 ===
    private int bonusHealth = 0;
    private int bonusShield = 0;
    private float healOnKill = 0f;
    private float ultChargeOnKill = 0f;
    private float ultimateChargeBonus = 0f;

    private int doomRoundCounter = 0;
    private int doomRoundTrigger = 0;
    private float doomRoundMultiplier = 1f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // =========================
    // === 기본 스탯 함수 ===
    // =========================
    public void AddDamageMultiplier(float value)
    {
        DamageMultiplier += value;
        Debug.Log($"[StatModifier] DamageMultiplier 현재값: {DamageMultiplier}");
    }

    public void AddFireRateMultiplier(float value)
    {
        FireRateMultiplier += value;
        Debug.Log($"[StatModifier] FireRateMultiplier 현재값: {FireRateMultiplier}");
    }

    public void AddMoveSpeedMultiplier(float value)
    {
        MoveSpeedMultiplier += value;
        Debug.Log($"[StatModifier] MoveSpeedMultiplier 현재값: {MoveSpeedMultiplier}");
    }

    // =========================
    // === 체력 / 실드 / 궁극기 ===
    // =========================
    public void AddHealth(int amount)
    {
        bonusHealth += amount;
        var player = FindObjectOfType<Character>();
        if (player != null)
        {
            player.Heal(amount);
            Debug.Log($"[StatModifier] 체력 {amount} 회복 / 총합 {bonusHealth}");
        }
    }

    public void AddShield(int amount)
    {
        bonusShield += amount;
        var player = FindObjectOfType<Character>();
        if (player != null)
        {
            player.RestoreShield(amount);
            Debug.Log($"[StatModifier] 실드 {amount} 회복 / 총합 {bonusShield}");
        }
    }

    public void AddUltimateCharge(float amount)
    {
        ultimateChargeBonus += amount;
        var ult = FindObjectOfType<UltimateSkill>();
        if (ult != null)
        {
            ult.AddGauge(amount);
            Debug.Log($"[StatModifier] 궁극기 게이지 +{amount}");
        }
    }

    // =========================
    // === DoomRound (5번째 탄 2배) ===
    // =========================
    public void RegisterDoomRound(int triggerCount, float multiplier)
    {
        doomRoundTrigger = triggerCount;
        doomRoundMultiplier = multiplier;
    }

    public float GetNextShotDamageMultiplier()
    {
        doomRoundCounter++;
        if (doomRoundTrigger > 0 && doomRoundCounter % doomRoundTrigger == 0)
            return doomRoundMultiplier;
        return 1f;
    }

    // Wrapper (AugmentSystem 호환용)
    public float NextShotDamageMultiplier()
    {
        return GetNextShotDamageMultiplier();
    }

    // =========================
    // === 다음 탄 크리티컬 확정 ===
    // =========================
    public void GuaranteeNextShotCrit() => nextShotCritGuaranteed = true;

    public void NextShotCritGuaranteed()
    {
        GuaranteeNextShotCrit();
    }

    public bool IsNextShotCritGuaranteed()
    {
        bool temp = nextShotCritGuaranteed;
        nextShotCritGuaranteed = false;
        return temp;
    }

    // =========================
    // === 다음 탄 폭발 ===
    // =========================
    public void NextShotExplosive()
    {
        nextShotExplosive = true;
    }

    public bool ConsumeNextShotExplosive()
    {
        bool temp = nextShotExplosive;
        nextShotExplosive = false;
        return temp;
    }

    // =========================
    // === 적 처치 / 공격 명중 ===
    // =========================
    public void OnEnemyKilled()
    {
        var player = FindObjectOfType<Character>();
        if (player != null && healOnKill > 0f)
        {
            player.Heal(healOnKill);
            Debug.Log($"[StatModifier] 적 처치 → 체력 {healOnKill} 회복");
        }

        var ult = FindObjectOfType<UltimateSkill>();
        if (ult != null && ultimateChargeBonus > 0f)
        {
            ult.AddGauge(ultimateChargeBonus);
            Debug.Log($"[StatModifier] 적 처치 → 궁극기 게이지 +{ultimateChargeBonus}");
        }
    }

    public void OnPlayerHitEnemy()
    {
        var ult = FindObjectOfType<UltimateSkill>();
        if (ult != null && ultChargeOnKill > 0f)
        {
            ult.AddGauge(ultChargeOnKill);
            Debug.Log($"[StatModifier] 공격 명중 → 궁극기 게이지 +{ultChargeOnKill}");
        }
        OnBulletHit();
    }
    // 치명타 확률 증가
    public void AddCriticalChance(float amount)
    {
        var character = FindObjectOfType<Character>();
        if (character != null)
        {
            character.CriticalChance += amount;

            Debug.Log($"[StatModifier] 치명타 확률 +{amount * 100}%");
        }
    }

    // 처치 시 회복량 증가
    public void AddHealOnKill(float amount)
    {
        healOnKill += amount;
        Debug.Log($"[StatModifier] HealOnKill +{amount}");
    }
    public void TriggerRush(float value)
    {
        AddFireRateMultiplier(value);
        Debug.Log("[증강] TriggerRush 발동: 공격속도 증가");
    }

    public void KillerInstinct(float value)
    {
        AddMoveSpeedMultiplier(value);
        Debug.Log("[증강] KillerInstinct 발동: 이동속도 증가");
    }

    public void Vengeance(float value)
    {
        AddDamageMultiplier(value);
        Debug.Log("[증강] Vengeance 발동: 다음 공격 데미지 증가");
    }

    public void AdrenalSurge(float value)
    {
        AddUltimateCharge(value);
        Debug.Log("[증강] AdrenalSurge 발동: 궁극기 충전");
    }

    public void ChainReaction(float value)
    {
        NextShotExplosive(); // 다음 탄 폭발로 지정
        Debug.Log("[증강] ChainReaction 발동: 폭발 탄환");
    }

    public void BulletFever(float value)
    {
        // 이미 구현돼 있는 BulletFever hit count 시스템 사용
        Debug.Log("[증강] BulletFever 발동 준비");
    }

    public void BloodFocus(float value)
    {
        AddCriticalChance(value);
        Debug.Log("[증강] BloodFocus 발동: 체력이 낮을 때 치명타 증가");
    }

    public void Retaliation(float value)
    {
        AddDamageMultiplier(value);
        Debug.Log("[증강] Retaliation 발동: 피격 후 공격력 증가");
    }

    public void ColdRage(float value)
    {
        AddDamageMultiplier(value);
        Debug.Log("[증강] ColdRage 발동: 저체온 버프 효과");
    }

    // =========================
    // === 명중 누적 (BulletFever 등) ===
    // =========================
    private int hitCount = 0;
    private int hitsForBuff = 10;
    private float feverBuffDuration = 5f;
    private bool feverActive = false;

    public void OnBulletHit()
    {
        hitCount++;
        if (hitCount >= hitsForBuff && !feverActive)
        {
            hitCount = 0;
            feverActive = true;
            AddFireRateMultiplier(0.2f);
            Debug.Log("[StatModifier] BulletFever 발동! 공격속도 +20%");
            StartCoroutine(RemoveFeverBuffAfterDelay());
        }
    }

    private System.Collections.IEnumerator RemoveFeverBuffAfterDelay()
    {
        yield return new WaitForSeconds(feverBuffDuration);
        FireRateMultiplier -= 0.2f;
        feverActive = false;
        Debug.Log("[StatModifier] BulletFever 종료");
    }

    // =========================
    // === 리셋 / 초기화 ===
    // =========================
    public void ResetAll()
    {
        DamageMultiplier = 1f;
        FireRateMultiplier = 1f;
        MoveSpeedMultiplier = 1f;
        nextShotCritGuaranteed = false;
        nextShotExplosive = false;
        nextShotDamageMultiplier = 1f;
        bonusHealth = 0;
        bonusShield = 0;
        healOnKill = 0;
        ultChargeOnKill = 0;
        ultimateChargeBonus = 0;
        Debug.Log("[StatModifier] 모든 버프 초기화됨");
    }
}
