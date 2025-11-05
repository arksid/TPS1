using UnityEngine;
using TMPro;

public class TutorialUI : MonoBehaviour
{
    [Header("연결 (TMP)")]
    public TextMeshProUGUI title;
    public TextMeshProUGUI hint;

    [Header("표시 옵션")]
    public CanvasGroup group;
    public float fadeSpeed = 8f;

    string _title, _hint;
    bool _visible;

    void Awake()
    {
        if (!group) group = GetComponentInChildren<CanvasGroup>(true);
        SetVisible(false, true);
    }

    public void Show(string titleText, string hintText)
    {
        _title = titleText;
        _hint = hintText;

        if (title) title.text = _title ?? "";
        if (hint) hint.text = _hint ?? "";

        SetVisible(true);
    }

    public void Hide() => SetVisible(false);

    void SetVisible(bool on, bool instant = false)
    {
        _visible = on;
        if (!group) return;

        if (instant)
            group.alpha = on ? 1f : 0f;
    }

    void Update()
    {
        if (!group) return;
        float target = _visible ? 1f : 0f;
        group.alpha = Mathf.MoveTowards(group.alpha, target, fadeSpeed * Time.deltaTime);
        group.blocksRaycasts = group.interactable = _visible;
    }
}
