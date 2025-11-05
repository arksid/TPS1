// Assets/Scripts/System/LoadingScreen.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingScreen : MonoBehaviour
{
    public Slider progressBar;                  // 0~1
    public TextMeshProUGUI percentText;         // "85%"
    public CanvasGroup canvasGroup;             // 페이드용(없으면 SceneLoader가 자동으로 붙임)

    public void SetProgress(float t)
    {
        t = Mathf.Clamp01(t);
        if (progressBar) progressBar.value = t;
        if (percentText) percentText.text = Mathf.RoundToInt(t * 100f) + "%";
    }
}
