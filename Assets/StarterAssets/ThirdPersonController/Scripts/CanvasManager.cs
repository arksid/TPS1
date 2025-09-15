using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasManager : MonoBehaviour
{
    public GameObject aimUI;
    public Slider healthSlider;
    public TMP_Text weaponNameText;
    public TMP_Text ammoText;

    [Header("Crosshair")]
    [SerializeField] private CrosshairController crosshair;

    [Header("Reload UI")]
    [SerializeField] private Image reloadRadial;      // 타입: Image, Filled, Radial360
    [SerializeField] private bool showIndeterminateWhenNoDuration = true;

    [Header("Roll Cooldown")]
    [SerializeField] private Image rollCooldownRadial;

    [Header("Hit Marker")]
    [SerializeField] private CanvasGroup hitMarker;
    [SerializeField] private float hitMarkerFadeIn = 18f;
    [SerializeField] private float hitMarkerHold = 0.05f;
    [SerializeField] private float hitMarkerFadeOut = 12f;
    [SerializeField] private CanvasGroup critMarker;  // 선택: 헤드샷용 별도 마커

    [Header("Damage Vignette")]
    [SerializeField] private CanvasGroup damageVignette;
    [SerializeField] private float damageFlashIn = 18f;
    [SerializeField] private float damageFlashOut = 2.5f;
    [SerializeField] private float lowHealthThreshold = 0.25f; // 25%
    [SerializeField] private AnimationCurve lowHealthPulse = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Interaction Prompt")]
    [SerializeField] private TMP_Text interactionPrompt;

    public static CanvasManager singleton;

    private Coroutine _hitRoutine;
    private Coroutine _vignetteRoutine;
    private float _lowHealthPulseT = 0f;
    private bool _reloading = false;
    private bool _reloadHasDuration = false;
    private float _reloadDuration = 0f;
    private float _reloadTimer = 0f;

    private void Awake()
    {
        singleton = this;
        if (reloadRadial) { reloadRadial.fillAmount = 0f; reloadRadial.gameObject.SetActive(false); }
        if (rollCooldownRadial) { rollCooldownRadial.fillAmount = 0f; rollCooldownRadial.gameObject.SetActive(true); }
        if (hitMarker) hitMarker.alpha = 0f;
        if (critMarker) critMarker.alpha = 0f;
        if (damageVignette) damageVignette.alpha = 0f;
        if (interactionPrompt) interactionPrompt.text = "";
    }

    private void Update()
    {
        // 재장전 인디케이터(지속시간 모르는 경우 도는 애니메이션 느낌)
        if (_reloading && reloadRadial)
        {
            if (_reloadHasDuration)
            {
                _reloadTimer += Time.deltaTime;
                reloadRadial.fillAmount = Mathf.Clamp01(_reloadTimer / Mathf.Max(0.01f, _reloadDuration));
            }
            else if (showIndeterminateWhenNoDuration)
            {
                // 간단 인디케이터: 맴도는 느낌
                reloadRadial.fillAmount = Mathf.PingPong(Time.time * 0.8f, 1f);
            }
        }

        // 저체력 펄스
        if (damageVignette && healthSlider)
        {
            float hp01 = Mathf.Approximately(healthSlider.maxValue, 0f) ? 0f : (healthSlider.value);
            if (hp01 <= lowHealthThreshold)
            {
                _lowHealthPulseT += Time.deltaTime;
                float pulse = lowHealthPulse.Evaluate((_lowHealthPulseT % 1f));
                damageVignette.alpha = Mathf.Max(damageVignette.alpha, Mathf.Lerp(0f, 0.4f, pulse));
            }
            else
            {
                _lowHealthPulseT = 0f;
            }
        }
    }

    // ===== 기존 UI =====
    public void HideAimUI() { if (aimUI) aimUI.SetActive(false); }
    public void ShowAimUI() { if (aimUI) aimUI.SetActive(true); }

    public void UpdateHealth(int current, int max)
    {
        if (healthSlider != null)
            healthSlider.value = (float)current / Mathf.Max(1, max);
    }

    public void UpdateWeapon(string weaponName)
    {
        if (weaponNameText != null)
            weaponNameText.text = weaponName;
    }

    public void UpdateAmmo(int current, int total)
    {
        if (ammoText != null)
            ammoText.text = $"{current} / {total}";
    }

    // ===== 크로스헤어 =====
    public void UpdateCrosshair(float degrees, bool aiming, bool visible)
    {
        if (crosshair != null)
        {
            if (aimUI && aimUI.activeSelf != visible) aimUI.SetActive(visible);
            crosshair.SetSpreadDegrees(degrees, aiming, visible);
        }
        else
        {
            if (aimUI != null) aimUI.SetActive(visible);
        }
    }

    // ===== 재장전 =====
    public void StartReloadUI(float duration = -1f)
    {
        _reloading = true;
        _reloadHasDuration = duration > 0f;
        _reloadDuration = duration;
        _reloadTimer = 0f;
        if (reloadRadial) reloadRadial.gameObject.SetActive(true);
    }

    public void StopReloadUI()
    {
        _reloading = false;
        if (reloadRadial)
        {
            reloadRadial.fillAmount = 0f;
            reloadRadial.gameObject.SetActive(false);
        }
    }

    // ===== 구르기 쿨다운 =====
    // t = 0~1 (0은 준비 완료, 1은 막 쿨다운 시작했다고 가정)
    public void UpdateRollCooldown(float t)
    {
        if (!rollCooldownRadial) return;
        rollCooldownRadial.fillAmount = Mathf.Clamp01(1f - t); // 바깥에서 t 증가 → UI는 감소 표시
    }

    // ===== 적중표시 =====
    public void FlashHitmarker(bool crit = false)
    {
        if (crit && critMarker != null)
        {
            if (_hitRoutine != null) StopCoroutine(_hitRoutine);
            _hitRoutine = StartCoroutine(CoFlash(critMarker));
        }
        else if (hitMarker != null)
        {
            if (_hitRoutine != null) StopCoroutine(_hitRoutine);
            _hitRoutine = StartCoroutine(CoFlash(hitMarker));
        }
    }

    private IEnumerator CoFlash(CanvasGroup cg)
    {
        cg.gameObject.SetActive(true);
        // in
        while (cg.alpha < 1f)
        {
            cg.alpha = Mathf.MoveTowards(cg.alpha, 1f, hitMarkerFadeIn * Time.deltaTime);
            yield return null;
        }
        // hold
        yield return new WaitForSeconds(hitMarkerHold);
        // out
        while (cg.alpha > 0f)
        {
            cg.alpha = Mathf.MoveTowards(cg.alpha, 0f, hitMarkerFadeOut * Time.deltaTime);
            yield return null;
        }
        cg.gameObject.SetActive(false);
    }

    // ===== 데미지 비네트 =====
    public void FlashDamage(float intensity01 = 1f)
    {
        if (!damageVignette) return;
        if (_vignetteRoutine != null) StopCoroutine(_vignetteRoutine);
        _vignetteRoutine = StartCoroutine(CoDamage(Mathf.Clamp01(intensity01)));
    }

    private IEnumerator CoDamage(float intensity)
    {
        damageVignette.gameObject.SetActive(true);
        float target = Mathf.Lerp(0.2f, 0.6f, intensity);
        // in
        while (damageVignette.alpha < target)
        {
            damageVignette.alpha = Mathf.MoveTowards(damageVignette.alpha, target, damageFlashIn * Time.deltaTime);
            yield return null;
        }
        // out
        while (damageVignette.alpha > 0f)
        {
            damageVignette.alpha = Mathf.MoveTowards(damageVignette.alpha, 0f, damageFlashOut * Time.deltaTime);
            yield return null;
        }
        damageVignette.gameObject.SetActive(false);
    }

    // ===== 상호작용 =====
    public void ShowInteraction(string text)
    {
        if (interactionPrompt)
        {
            interactionPrompt.text = text ?? "";
            interactionPrompt.gameObject.SetActive(!string.IsNullOrEmpty(interactionPrompt.text));
        }
    }
    public void HideInteraction()
    {
        if (interactionPrompt)
        {
            interactionPrompt.text = "";
            interactionPrompt.gameObject.SetActive(false);
        }
    }
}
