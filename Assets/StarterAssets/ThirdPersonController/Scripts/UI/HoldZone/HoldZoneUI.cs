using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HoldZoneUI : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public Slider slider;              // 0~1
    public TextMeshProUGUI hintLabel;  // 안내 문구

    public void Show()
    {
        if (canvasGroup)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
    }

    public void SetProgress(float t01)
    {
        if (slider) slider.value = Mathf.Clamp01(t01);
    }

    public void SetHint(string msg)
    {
        if (hintLabel) hintLabel.text = msg;
    }
}
