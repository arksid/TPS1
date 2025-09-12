using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private CrosshairController crosshair; // ★ 추가

    public static CanvasManager singleton;

    private void Awake()
    {
        singleton = this;
    }

    public void HideAimUI()
    {
        if (aimUI != null)
            aimUI.SetActive(false);
    }
    public void ShowAimUI()
    {
        if (aimUI != null)
            aimUI.SetActive(true);
    }
    public void UpdateHealth(int current, int max)
    {
        if (healthSlider != null)
            healthSlider.value = (float)current / max;
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

    // ★ 신규: 크로스헤어 갱신 (degrees = 무기 총 퍼짐)
    public void UpdateCrosshair(float degrees, bool aiming, bool visible)
    {
        if (crosshair != null)
        {
            // aimUI가 있다면 함께 표시 상태 동기화(선택)
            if (aimUI != null && aimUI.activeSelf != visible)
                aimUI.SetActive(visible);

            crosshair.SetSpreadDegrees(degrees, aiming, visible);
        }
        else
        {
            // fallback: aimUI만 on/off
            if (aimUI != null)
                aimUI.SetActive(visible);
        }
    }
}
