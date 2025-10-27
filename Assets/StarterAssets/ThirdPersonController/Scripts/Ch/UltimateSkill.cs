using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class UltimateSkill : MonoBehaviour
{// 클래스 상단 private 필드 추가
    private Coroutine ultimateUiDrainRoutine;

    [Header("궁극기 설정")]
    public float ultimateDuration = 20f;
    [Range(0.05f, 1f)] public float slowFactor = 0.2f;
    public float damageMultiplier = 2f;
    public float fireRateMultiplier = 0.5f;

    [Header("게이지 설정")]
    [SerializeField] private float maxGauge = 100f;
    [SerializeField] private float currentGauge = 0f;
    [SerializeField] public float gaugePerHit = 5f;

    [Header("시각 효과 설정")]
    public Volume ultimateVolume;            // Global Volume 연결
    public float volumeFadeSpeed = 2f;

    private Coroutine volumeRoutine;
    private bool isActive;

    // 전역 상태
    public static bool IsUltimateActive { get; private set; }
    public static float CurrentSlowFactor { get; private set; } = 1f;
    public static float CurrentDamageMultiplier { get; private set; } = 1f;
    public static float CurrentFireRateMultiplier { get; private set; } = 1f;

    public float GaugePerHit => gaugePerHit;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) && !isActive && currentGauge >= maxGauge)
        {
            ActivateUltimate();
        }
    }

    private void ActivateUltimate()
    {
        isActive = true;
        IsUltimateActive = true;
        CurrentSlowFactor = slowFactor;
        CurrentDamageMultiplier = damageMultiplier;
        CurrentFireRateMultiplier = fireRateMultiplier;

        currentGauge = 0f;
        if (CanvasManager.singleton != null)
        {
            // 궁극기 발동 직후: UI를 "남은 시간" 표시 모드로 100%로 채워놓기
            CanvasManager.singleton.UpdateUltimateGauge(1f);
            CanvasManager.singleton.UpdateUltimatePercentByRatio(1f);
        }

        // 🐢 슬로우 적용 (유지)
        foreach (var mb in FindObjectsOfType<MonoBehaviour>(true))
        {
            if (mb is ISlowable s && ShouldBeSlowed(mb))
                s.SetLocalTimeScale(slowFactor);
        }

        // ✨ Outline 켜기 (적만)
        foreach (var outline in FindObjectsOfType<Outline>(true))
        {
            if (outline.gameObject.CompareTag("Enemy"))
                outline.enabled = true;
        }

        // 🌈 볼륨 페이드 인 추가
        if (ultimateVolume != null)
        {
            if (volumeRoutine != null) StopCoroutine(volumeRoutine);
            volumeRoutine = StartCoroutine(FadeVolume(1f));
        }


        StartCoroutine(EndUltimate());
        // ActivateUltimate() 맨 끝, StartCoroutine(EndUltimate()); 바로 위 또는 아래 아무 곳에
        if (ultimateUiDrainRoutine != null) StopCoroutine(ultimateUiDrainRoutine);
        ultimateUiDrainRoutine = StartCoroutine(DrainUltimateUi());

    }
    private IEnumerator DrainUltimateUi()
    {
        float elapsed = 0f;
        while (elapsed < ultimateDuration)
        {
            elapsed += Time.deltaTime;
            float remain = Mathf.Clamp01(1f - (elapsed / ultimateDuration)); // 1 → 0
            if (CanvasManager.singleton != null)
            {
                // ✅ 슬라이더만 갱신 (CanvasManager.UpdateUltimateGauge 내부가 퍼센트까지 처리)
                CanvasManager.singleton.UpdateUltimateGauge(remain);
            }
            yield return null;
        }
    }

    private IEnumerator EndUltimate()
    {
        yield return new WaitForSeconds(ultimateDuration);

        // 🐢 속도 원복 (유지)
        foreach (var mb in FindObjectsOfType<MonoBehaviour>(true))
        {
            if (mb is ISlowable s && ShouldBeSlowed(mb))
                s.SetLocalTimeScale(1f);
        }

        // ✨ Outline 끄기 (모두 안전하게 끔)
        foreach (var outline in FindObjectsOfType<Outline>(true))
        {
            outline.enabled = false;
        }



        IsUltimateActive = false;
        CurrentSlowFactor = 1f;
        CurrentDamageMultiplier = 1f;
        CurrentFireRateMultiplier = 1f;
        isActive = false;

        if (ultimateVolume != null)
        {
            if (volumeRoutine != null) StopCoroutine(volumeRoutine);
            volumeRoutine = StartCoroutine(FadeVolume(0f));
        }
        if (ultimateUiDrainRoutine != null)
        {
            StopCoroutine(ultimateUiDrainRoutine);
            ultimateUiDrainRoutine = null;
        }
        if (CanvasManager.singleton != null)
        {
            // 궁극기가 끝나면 남은시간 UI는 0%로 마무리
            CanvasManager.singleton.UpdateUltimateGauge(0f);
            CanvasManager.singleton.UpdateUltimatePercentByRatio(0f);
        }

    }

    private IEnumerator FadeVolume(float targetWeight)
    {
        float startWeight = ultimateVolume.weight;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * volumeFadeSpeed;
            ultimateVolume.weight = Mathf.Lerp(startWeight, targetWeight, t);
            yield return null;
        }

        ultimateVolume.weight = targetWeight;
    }

    private bool ShouldBeSlowed(MonoBehaviour mb)
    {
        // 플레이어 제외
        if (mb.CompareTag("Player")) return false;
        // 적, 적 총알만 슬로우
        if (mb.CompareTag("Enemy") || mb.CompareTag("EnemyProjectile")) return true;
        return false;
    }

    public void AddGauge(float amount)
    {
        // ⛔ 궁극기 지속 중에는 게이지가 차지 않게 막기
        if (IsUltimateActive) return;

        currentGauge = Mathf.Clamp(currentGauge + amount, 0f, maxGauge);
        if (CanvasManager.singleton != null)
            CanvasManager.singleton.UpdateUltimateGauge(currentGauge / maxGauge);
    }
}

public interface ISlowable
{
    void SetLocalTimeScale(float scale);
}
