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
    [SerializeField] private GameObject crosshair;   // 🔥 추가

    public static CanvasManager singleton;

    private Coroutine reloadRoutine;

    private void Awake()
    {
        singleton = this;
        if (reloadRadial) reloadRadial.fillAmount = 0f;
    }

    private void UpdateHealthUI(int current, int max)
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
    }

    // ===== 체력 =====
    public void UpdateHealth(int current, int max) => UpdateHealthUI(current, max);

    // ===== 무기 & 탄약 =====
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

    // ===== 재장전 =====
    public void StartReloadUI(float duration)
    {
        if (reloadRoutine != null) StopCoroutine(reloadRoutine);
        reloadRoutine = StartCoroutine(ReloadAnimation(duration));

        // 🔥 재장전 시작 → 크로스헤어 숨기기
        if (crosshair) crosshair.SetActive(false);
    }

    public void StopReloadUI()
    {
        if (reloadRoutine != null) StopCoroutine(reloadRoutine);
        reloadRoutine = null;

        if (reloadRadial) reloadRadial.fillAmount = 0f;

        // 🔥 재장전 끝 → 크로스헤어 다시 보이기
        if (crosshair) crosshair.SetActive(true);
    }

    private IEnumerator ReloadAnimation(float duration)
    {
        if (reloadRadial == null) yield break;

        float elapsed = 0f;
        float cycle = 0.2f; // 0.2초마다 애니메이션 반복

        while (elapsed < duration)
        {
            float t = 0f;
            while (t < cycle)
            {
                t += Time.deltaTime;
                elapsed += Time.deltaTime;

                // 위→아래 채워지게 (Fill Origin = Top, Fill Method = Vertical 로 설정 필요)
                reloadRadial.fillAmount = Mathf.Clamp01(t / cycle);

                yield return null;
                if (elapsed >= duration) break;
            }

            // 한 사이클 끝나면 0으로 리셋
            reloadRadial.fillAmount = 0f;
        }

        // 재장전 끝나면 UI 리셋
        StopReloadUI();
    }
}
