using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CanvasManager : MonoBehaviour
{
    [Header("Health UI")]
    public Slider healthSlider;
    [SerializeField] private TMP_Text healthText;

    [Header("Shield UI ")]
    [SerializeField] private Slider shieldSlider;
    [SerializeField] private TMPro.TMP_Text shieldText;

    [Header("Experience UI ✨")]
    [SerializeField] private Slider expSlider;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private TMP_Text expRemainText;
    [SerializeField] private float expLerpSpeed = 5f; // 게이지 부드럽게 따라가는 속도
    private float targetExpValue = 0f; // 목표 경험치 값

    [Header("Weapon UI")]
    public TMP_Text weaponNameText;
    public TMP_Text ammoText;

    [Header("Reload UI")]
    [SerializeField] private Image reloadRadial;

    [Header("Crosshair UI")]
    [SerializeField] private GameObject crosshair;

    public static CanvasManager singleton;

    private Coroutine reloadRoutine;
    private Coroutine ammoAnimRoutine;

    [Header("Damage UI")]
    [SerializeField] private TMPro.TMP_Text damageText;
    private Coroutine damageAnimRoutine;

    [Header("Healing Item UI")]
    [SerializeField] private TMPro.TMP_Text healingItemCountText;
    [SerializeField] private UnityEngine.UI.Image healingItemIcon;     // 🩹 아이콘
    [SerializeField] private UnityEngine.UI.Image healingItemPanelBG;  // 🪄 배경
    private Coroutine healingItemAnimRoutine;
    [Header("Ultimate")]
    [SerializeField] private UnityEngine.UI.Image ultimateOverlay;
    [SerializeField] private UnityEngine.UI.Slider ultimateGaugeSlider;
    private void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Destroy(gameObject);
            return;
        }
        singleton = this;
    }
    private void Update()
    {
        // EXP 게이지 부드럽게 이동
        if (expSlider != null)
        {
            expSlider.value = Mathf.Lerp(expSlider.value, targetExpValue, Time.deltaTime * expLerpSpeed);
        }
    }
    // ===== 체력 =====
    public void UpdateHealth(int current, int max)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value = current;
        }

        if (healthText != null)
            healthText.text = $"HP : {current} / {max}";
    }
    // ===== 무기 & 탄약 =====
    public void UpdateWeapon(string weaponName)
    {
        if (weaponNameText != null)
            weaponNameText.text = weaponName;
    }

    public void UpdateAmmo(int current, int total)
    {
        if (ammoText != null)
        {
            ammoText.text = $"{current} / {total}";

            if (ammoAnimRoutine != null)
                StopCoroutine(ammoAnimRoutine);

            ammoAnimRoutine = StartCoroutine(AmmoTextPop(ammoText));
        }
    }
    // ✨ 경험치 UI
    // ================================
    public void UpdateExpUI(int currentExp, int expToNextLevel)
    {
        if (expSlider != null)
        {
            expSlider.maxValue = expToNextLevel;
            targetExpValue = currentExp; // 목표치만 변경하고 실제 값은 Lerp로 부드럽게 이동
        }

        if (expText != null)
            expText.text = $"EXP : {currentExp} / {expToNextLevel}";

        if (expRemainText != null)
        {
            int remain = Mathf.Max(expToNextLevel - currentExp, 0);
            expRemainText.text = $"남은 경험치 : {remain}";
        }
    }

public void UpdateShield(int current, int max)
    {
        if (shieldSlider != null)
        {
            shieldSlider.maxValue = max;
            shieldSlider.value = current;
        }

        if (shieldText != null)
        {
            if (max > 0)
                shieldText.text = $"SD : {current} / {max}";
            else
                shieldText.text = "SD : 0 / 0";
        }
    }

    private IEnumerator AmmoTextPop(TMP_Text text)
    {
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = originalScale * 1.3f;
        float duration = 0.1f;

        Color originalColor = Color.white;
        Color flashColor = Color.red;

        // 항상 원래 상태에서 시작
        text.rectTransform.localScale = originalScale;
        text.color = originalColor;

        // 커지면서 빨갛게
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;

            text.rectTransform.localScale = Vector3.Lerp(originalScale, targetScale, lerp);
            text.color = Color.Lerp(originalColor, flashColor, lerp);

            yield return null;
        }

        // 살짝 유지
        yield return new WaitForSeconds(0.05f);

        // 줄어들면서 색 복구
        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;

            text.rectTransform.localScale = Vector3.Lerp(targetScale, originalScale, lerp);
            text.color = Color.Lerp(flashColor, originalColor, lerp);

            yield return null;
        }

        // 안전하게 원래 상태로
        text.rectTransform.localScale = originalScale;
        text.color = originalColor;
    }

    // ===== 재장전 =====
    public void StartReloadUI(float duration)
    {
        if (reloadRoutine != null) StopCoroutine(reloadRoutine);
        reloadRoutine = StartCoroutine(ReloadAnimation(duration));

        if (crosshair) crosshair.SetActive(false);
    }

    public void StopReloadUI()
    {
        if (reloadRoutine != null) StopCoroutine(reloadRoutine);
        reloadRoutine = null;

        if (reloadRadial) reloadRadial.fillAmount = 0f;
        if (crosshair) crosshair.SetActive(true);
    }

    private IEnumerator ReloadAnimation(float duration)
    {
        if (reloadRadial == null) yield break;

        float elapsed = 0f;
        float cycle = 0.2f;

        while (elapsed < duration)
        {
            float t = 0f;
            while (t < cycle)
            {
                t += Time.deltaTime;
                elapsed += Time.deltaTime;

                reloadRadial.fillAmount = Mathf.Clamp01(t / cycle);

                yield return null;
                if (elapsed >= duration) break;
            }
            reloadRadial.fillAmount = 0f;
        }

        StopReloadUI();
    }

    public void ShowDamage(float damage)
    {
        if (damageText == null) return;

        damageText.gameObject.SetActive(true);
        damageText.text = $"-{Mathf.RoundToInt(damage)}";

        if (damageAnimRoutine != null)
            StopCoroutine(damageAnimRoutine);

        damageAnimRoutine = StartCoroutine(DamageTextPop(damageText));
    }

    private IEnumerator DamageTextPop(TMPro.TMP_Text text)
    {
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = originalScale * 1.6f;
        float duration = 0.15f;
        float fadeTime = 0.5f;

        Color originalColor = Color.red;
        Color flashColor = Color.white;
    

        text.rectTransform.localScale = originalScale;
        text.color = flashColor;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;
            text.rectTransform.localScale = Vector3.Lerp(originalScale, targetScale, lerp);
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        float fade = 0f;
        while (fade < fadeTime)
        {
            fade += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, fade / fadeTime);
            Color c = text.color;
            c.a = alpha;
            text.color = c;
            yield return null;
        }

        text.gameObject.SetActive(false);
        text.rectTransform.localScale = originalScale;
        text.color = originalColor;
    }
    public void UpdateHealingItemCount(int count)
    {
        if (healingItemCountText != null)
        {
            healingItemCountText.text = $"HP x {count}";

            // 숫자 애니메이션
            if (healingItemAnimRoutine != null)
                StopCoroutine(healingItemAnimRoutine);
            healingItemAnimRoutine = StartCoroutine(HealingItemPop(healingItemCountText));
        }

        // 아이콘과 배경 상태 업데이트
        if (healingItemIcon != null && healingItemPanelBG != null)
        {
            if (count > 0)
            {
                healingItemIcon.color = Color.white;
                healingItemPanelBG.color = new Color(1f, 1f, 1f, 0.5f); // 반투명 흰색
            }
            else
            {
                healingItemIcon.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 회색 처리
                healingItemPanelBG.color = new Color(0.2f, 0.2f, 0.2f, 0.3f); // 어두운 색
            }
        }
    }

    private IEnumerator HealingItemPop(TMPro.TMP_Text text)
    {
        Vector3 originalScale = Vector3.one;                        // 원본 스케일 고정
        Vector3 targetScale = originalScale * 1.3f;                 // 커질 크기
        float duration = 0.15f;

        // ✅ 시작할 때 무조건 원래 크기로 리셋
        text.rectTransform.localScale = originalScale;

        // 커지기
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;
            text.rectTransform.localScale = Vector3.Lerp(originalScale, targetScale, lerp);
            yield return null;
        }

        // 살짝 유지
        yield return new WaitForSeconds(0.05f);

        // 다시 줄이기
        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;
            text.rectTransform.localScale = Vector3.Lerp(targetScale, originalScale, lerp);
            yield return null;
        }

        text.rectTransform.localScale = originalScale;
    }

    public void SetUltimateOverlayAlpha(float alpha)
    {
        if (ultimateOverlay != null)
        {
            Color c = ultimateOverlay.color;
            c.a = alpha;
            ultimateOverlay.color = c;
        }
    }
    public void UpdateUltimateGauge(float ratio)
    {
        if (ultimateGaugeSlider != null)
        {
            ultimateGaugeSlider.value = ratio;
        }
    }

}
