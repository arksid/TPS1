using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AugmentOptionButton : MonoBehaviour
{
    [Header("UI References")]
    public Image icon;
    public TMP_Text nameText;
    public TMP_Text descText;
    public Button button;

    private AugmentData myData;

    public void Init(AugmentData data)
    {
        myData = data;

        if (icon != null && data.icon != null)
            icon.sprite = data.icon;

        if (nameText != null)
            nameText.text = data.augmentName;

        if (descText != null)
            descText.text = data.description;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                AugmentUIManager.Instance.OnPickAugment(myData);
            });
        }
    }
}
