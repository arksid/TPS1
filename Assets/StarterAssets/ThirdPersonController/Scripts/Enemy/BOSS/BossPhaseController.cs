using UnityEngine;

/// <summary>
/// 보스 HP 비율에 따라 BossWeaponMissile의 패턴을 자동 전환하는 컨트롤러.
/// - 70% 초과 : AimedSalvo
/// - 70%~40% : SweepLeftRight
/// - 40% 이하 : Rain + 분노(템포↑, 추가탄)
/// 패턴 전환 최소 유지시간/로그 등 안전장치 포함.
/// </summary>
public class BossMissilePhaseController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("미사일 무기 스크립트 (필수)")]
    public BossWeaponMissile missileWeapon;

    [Tooltip("보스 본체 (체력 비율 읽기). BossMonster가 없으면 null 허용")]
    public BossMonster boss; // boss.HpRatio 사용 (없으면 외부에서 SetManualHpRatio로 갱신)

    [Header("HP Thresholds")]
    [Range(0.1f, 0.99f)] public float phase2Threshold = 0.70f; // 70%
    [Range(0.05f, 0.90f)] public float phase3Threshold = 0.40f; // 40%

    [Header("Pattern Settings")]
    [Tooltip("패턴 전환 후 최소 유지 시간(초) – 깜빡임 방지")]
    public float minPatternHoldSeconds = 4.0f;

    [Tooltip("컨트롤러가 무기 패턴을 직접 지정할지(권장: 켜기). 끄면 무기 내부 루프 사용")]
    public bool controllerDrivesPatterns = true;

    [Header("Enrage")]
    public bool enableEnrageOnPhase3 = true;   // 40% 이하에서 분노 켜기
    public bool disableLoopOnController = true; // 컨트롤러가 패턴을 잡을 땐 무기 loopPatterns 끄기 권장

    [Header("Debug")]
    public bool enableLogging = true;

    // 내부 상태
    private int _currentPhase = 1;  // 1,2,3
    private float _lastSwitchTime = -999f;
    private float _manualHpRatio = 1f; // boss가 없을 때 외부에서 SetManualHpRatio로 갱신

    void Reset()
    {
        // 합리적 디폴트
        phase2Threshold = 0.70f;
        phase3Threshold = 0.40f;
        minPatternHoldSeconds = 4.0f;
        controllerDrivesPatterns = true;
        enableEnrageOnPhase3 = true;
        disableLoopOnController = true;
    }

    void OnEnable()
    {
        // 무기 안전 설정
        if (missileWeapon != null && controllerDrivesPatterns)
        {
            if (disableLoopOnController) missileWeapon.loopPatterns = false;
            // 시작 패턴 기본값 지정
            missileWeapon.currentPattern = BossWeaponMissile.Pattern.AimedSalvo;
            if (enableLogging) Debug.Log("[PhaseCtrl] OnEnable → set Pattern=AimedSalvo");
        }
    }

    void Update()
    {
        if (missileWeapon == null)
        {
            if (enableLogging) Debug.LogWarning("[PhaseCtrl] missileWeapon 미할당");
            return;
        }

        float hp = GetHpRatioSafe();

        // 현재 페이즈 계산
        int nextPhase = 1;
        if (hp <= phase3Threshold) nextPhase = 3;
        else if (hp <= phase2Threshold) nextPhase = 2;

        // 최소 유지시간(디바운스)
        if (nextPhase != _currentPhase && (Time.time - _lastSwitchTime) < minPatternHoldSeconds)
        {
            // 아직 전환 잠금 시간
            return;
        }

        if (nextPhase != _currentPhase)
        {
            _currentPhase = nextPhase;
            _lastSwitchTime = Time.time;
            ApplyPhaseSettings(_currentPhase);
        }
    }

    private float GetHpRatioSafe()
    {
        if (boss != null)
        {
            // BossMonster에 HpRatio 프로퍼티가 있다고 가정 (없으면 1로 처리)
            try
            {
                return Mathf.Clamp01(boss.HpRatio);
            }
            catch { return Mathf.Clamp01(_manualHpRatio); }
        }
        return Mathf.Clamp01(_manualHpRatio);
    }

    /// <summary>
    /// BossMonster가 없거나, 외부에서 커스텀으로 체력 비율을 갱신하고 싶을 때 호출.
    /// 0~1 범위로 넘겨주세요.
    /// </summary>
    public void SetManualHpRatio(float ratio01)
    {
        _manualHpRatio = Mathf.Clamp01(ratio01);
    }

    private void ApplyPhaseSettings(int phase)
    {
        if (enableLogging) Debug.Log($"[PhaseCtrl] ▶ 페이즈 전환: {phase}");

        // 무기 내부 루프를 끄고(선택), 컨트롤러가 패턴 직접 지정
        if (controllerDrivesPatterns)
        {
            if (disableLoopOnController) missileWeapon.loopPatterns = false;

            switch (phase)
            {
                case 1:
                    missileWeapon.currentPattern = BossWeaponMissile.Pattern.AimedSalvo;
                    // 평온 상태
                    missileWeapon.enraged = false;
                    break;

                case 2:
                    missileWeapon.currentPattern = BossWeaponMissile.Pattern.SweepLeftRight;
                    // 중간 난이도 – 약간 템포↑ 원하면 여기서 enraged On 가능
                    missileWeapon.enraged = false;
                    break;

                case 3:
                    // 막페이즈 – Rain으로 압박 또는 Pincer로 마무리
                    // 원하는 연출에 맞춰 아래 둘 중 하나 선택
                    // missileWeapon.currentPattern = BossWeaponMissile.Pattern.Rain;
                    missileWeapon.currentPattern = BossWeaponMissile.Pattern.Pincer;

                    if (enableEnrageOnPhase3)
                        missileWeapon.enraged = true; // 템포↑, 추가탄
                    break;
            }
            if (enableLogging) Debug.Log($"[PhaseCtrl] 무기 패턴 지정: {missileWeapon.currentPattern}, enraged={missileWeapon.enraged}");
        }
        else
        {
            // 무기 자체 루프를 쓰는 모드라면, 여기서는 분노만 제어
            if (phase == 3 && enableEnrageOnPhase3) missileWeapon.enraged = true;
            else missileWeapon.enraged = false;
        }
    }
}
