using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AugmentOptionButton : MonoBehaviour
{
    [Header("UI References")]
    public Image icon;
    public TMP_Text nameText;
    public TMP_Text descText;

    [Tooltip("증강 선택 버튼(기존 button)")]
    public Button button;

    [Tooltip("증강 새로고침 버튼(프리팹에 버튼 하나 추가해서 연결)")]
    public Button rerollButton;

    private AugmentData myData;

    /// <summary>
    /// 아이템 UI와 콜백을 한 번에 바인딩
    /// </summary>
    public void Bind(
        AugmentData data,
        System.Action onPick,
        System.Action onReroll,
        bool canReroll)
    {
        myData = data;

        if (icon != null && data != null && data.icon != null)
            icon.sprite = data.icon;

        if (nameText != null)
            nameText.text = data != null ? data.augmentName : "-";

        if (descText != null)
            descText.text = data != null ? data.description : "-";

        // 선택 버튼
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (onPick != null)
                button.onClick.AddListener(() => onPick());
        }

        // 새로고침 버튼
        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveAllListeners();
            rerollButton.interactable = canReroll;

            if (canReroll && onReroll != null)
                rerollButton.onClick.AddListener(() => onReroll());
        }
    }
}
