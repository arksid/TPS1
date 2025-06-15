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
}