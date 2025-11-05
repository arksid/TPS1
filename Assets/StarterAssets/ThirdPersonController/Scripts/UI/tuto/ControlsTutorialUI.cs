using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ControlsTutorialUI : MonoBehaviour
{
    [Header("연결 (TMP & Slider)")]
    public TextMeshProUGUI title;
    public TextMeshProUGUI hint;
    public Slider stepProgress;

    [Header("표시 옵션")]
    public CanvasGroup group;
    public float fadeSpeed = 8f;

    bool _visible;

    void Awake()
    {
        if (!group) group = GetComponent<CanvasGroup>();
        if (!group) group = gameObject.AddComponent<CanvasGroup>();
        SetVisible(false, true);
        if (stepProgress)
        {
            stepProgress.minValue = 0f;
            stepProgress.maxValue = 1f;
            stepProgress.value = 0f;
        }
    }

    public void SetText(string t, string h)
    {
        if (title) title.text = t ?? "";
        if (hint) hint.text = h ?? "";
    }

    public void SetProgress01(float p)
    {
        if (!stepProgress) return;
        stepProgress.value = Mathf.Clamp01(p);
    }

    public void Show() => SetVisible(true);
    public void Hide() => SetVisible(false);

    void SetVisible(bool on, bool instant = false)
    {
        _visible = on;
        if (!group) return;
        if (instant) { group.alpha = on ? 1f : 0f; return; }
        // 나머지는 Update에서 부드럽게
    }

    void Update()
    {
        if (!group) return;
        float target = _visible ? 1f : 0f;
        group.alpha = Mathf.MoveTowards(group.alpha, target, fadeSpeed * Time.deltaTime);
        group.blocksRaycasts = group.interactable = _visible;
    }
}
