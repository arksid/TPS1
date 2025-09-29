using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CanvasManager : MonoBehaviour
{
    [Header("Health UI")]
    public Slider healthSlider;
    [SerializeField] private TMP_Text healthText;

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

    private void Awake()
    {
        singleton = this;
        if (reloadRadial) reloadRadial.fillAmount = 0f;
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
            healthText.text = $"HP: {current} / {max}";
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
}
