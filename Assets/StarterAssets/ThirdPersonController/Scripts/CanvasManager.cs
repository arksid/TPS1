using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasManager : MonoBehaviour
{
    [Header("Basic UI")]
    public GameObject aimUI;

    public Slider healthSlider;
    [Header("Health Text")]
    [SerializeField] private TMP_Text healthText;

    public TMP_Text weaponNameText;
    public TMP_Text ammoText;

    [Header("Crosshair")]
    [SerializeField] private CrosshairController crosshair;

    [Header("Reload UI")]
    [SerializeField] private Image reloadRadial;

    [Header("Roll Cooldown")]
    [SerializeField] private Image rollCooldownRadial;

    [Header("Hit Marker")]
    [SerializeField] private CanvasGroup hitMarker;
    [SerializeField] private CanvasGroup critMarker;

    [Header("Damage Vignette")]
    [SerializeField] private CanvasGroup damageVignette;

    [Header("Interaction Prompt")]
    [SerializeField] private TMP_Text interactionPrompt;

    public static CanvasManager singleton;

    private bool _reloading = false;
    private float _reloadDuration = 0f;
    private float _reloadTimer = 0f;

    private Coroutine _hitRoutine;
    private Coroutine _vignetteRoutine;

    private void Awake()
    {
        singleton = this;
        if (reloadRadial) reloadRadial.fillAmount = 0f;
        if (rollCooldownRadial) rollCooldownRadial.fillAmount = 0f;
        if (hitMarker) hitMarker.alpha = 0f;
        if (critMarker) critMarker.alpha = 0f;
        if (damageVignette) damageVignette.alpha = 0f;
        if (interactionPrompt) interactionPrompt.text = "";
    }

    private void Update()
    {
        // 임시 테스트 입력
        if (Input.GetKeyDown(KeyCode.H)) CanvasManager.singleton?.FlashHitmarker();
        if (Input.GetKeyDown(KeyCode.R)) CanvasManager.singleton?.StartReloadUI(2f);
        if (Input.GetKeyDown(KeyCode.T)) CanvasManager.singleton?.StopReloadUI();
        if (Input.GetKeyDown(KeyCode.D)) CanvasManager.singleton?.FlashDamage(1f);

        if (_reloading && reloadRadial)
        {
            _reloadTimer += Time.deltaTime;
            reloadRadial.fillAmount = Mathf.Clamp01(_reloadTimer / Mathf.Max(0.01f, _reloadDuration));
        }
    }

    // ===== 기본 UI =====
    public void UpdateHealth(int current, int max)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value = current;
        }

        if (healthText != null)
        {
            healthText.text = $"HP: {current} / {max}";
        }

        Debug.Log($"[CanvasManager] Updated Health UI: {current} / {max}");
        healthText.text = current.ToString();
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
            crosshair.SetSpreadDegrees(degrees, aiming, visible);
        if (aimUI != null)
            aimUI.SetActive(visible);
    }

    // ===== 재장전 =====
    public void StartReloadUI(float duration)
    {
        _reloading = true;
        _reloadDuration = duration;
        _reloadTimer = 0f;
        if (reloadRadial) reloadRadial.fillAmount = 0f;
    }

    public void StopReloadUI()
    {
        _reloading = false;
        if (reloadRadial) reloadRadial.fillAmount = 0f;
    }

    // ===== 구르기 쿨다운 =====
    public void UpdateRollCooldown(float t)
    {
        if (rollCooldownRadial)
            rollCooldownRadial.fillAmount = Mathf.Clamp01(1f - t);
    }

    // ===== 히트마커 =====
    public void FlashHitmarker(bool crit = false)
    {
        if (_hitRoutine != null) StopCoroutine(_hitRoutine);
        _hitRoutine = StartCoroutine(CoFlash(crit ? critMarker : hitMarker));
    }

    private IEnumerator CoFlash(CanvasGroup cg)
    {
        if (cg == null) yield break;

        cg.alpha = 1f;
        yield return new WaitForSeconds(0.1f);

        while (cg.alpha > 0f)
        {
            cg.alpha -= Time.deltaTime * 5f;
            yield return null;
        }
    }

    // ===== 데미지 비네트 =====
    public void FlashDamage(float intensity01 = 1f)
    {
        if (_vignetteRoutine != null) StopCoroutine(_vignetteRoutine);
        _vignetteRoutine = StartCoroutine(CoDamage(intensity01));
    }

    private IEnumerator CoDamage(float intensity)
    {
        if (damageVignette == null) yield break;

        damageVignette.alpha = Mathf.Clamp01(intensity);
        yield return new WaitForSeconds(0.1f);

        while (damageVignette.alpha > 0f)
        {
            damageVignette.alpha -= Time.deltaTime * 2f;
            yield return null;
        }
    }

    // ===== 상호작용 =====
    public void ShowInteraction(string text)
    {
        if (interactionPrompt)
        {
            interactionPrompt.text = text;
            interactionPrompt.gameObject.SetActive(true);
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
