using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AugmentUIManager : MonoBehaviour
{
    public static AugmentUIManager Instance;

    [Header("UI References")]
    public GameObject augmentPanel;
    public Transform optionParent;
    public GameObject augmentOptionPrefab;

    [Header("Augment Pool")]
    public List<AugmentData> allAugments = new List<AugmentData>();

    private List<AugmentData> currentChoices = new List<AugmentData>();

    private void Awake()
    {
        Instance = this;
        augmentPanel.SetActive(false);
    }

    private Color GetRarityColor(AugmentRarity rarity)
    {
        switch (rarity)
        {
            case AugmentRarity.Rare: return new Color(0.2f, 0.6f, 1f);
            case AugmentRarity.Epic: return new Color(0.6f, 0.3f, 1f);
            case AugmentRarity.Legendary: return new Color(1f, 0.5f, 0f);
            default: return Color.white;
        }
    }

    private AugmentRarity GetRarityByChance()
    {
        float roll = Random.value;
        if (roll < 0.6f) return AugmentRarity.Normal;       // 60%
        else if (roll < 0.85f) return AugmentRarity.Rare;   // 25%
        else if (roll < 0.95f) return AugmentRarity.Epic;   // 10%
        else return AugmentRarity.Legendary;               // 5%
    }

    private AugmentCategory GetCategoryByChance()
    {
        float roll = Random.value;
        if (roll < 0.8f) return AugmentCategory.Normal;   // 🟢 80% 확률로 일반 특성
        else return AugmentCategory.Special;             // 🟡 20% 확률로 특수 특성
    }

    private AugmentData GetRandomAugment()
    {
        AugmentRarity targetRarity = GetRarityByChance();
        AugmentCategory targetCategory = GetCategoryByChance();

        // 필터링: 희귀도와 카테고리가 일치하는 특성만 뽑기
        List<AugmentData> pool = allAugments.FindAll(
            a => a.rarity == targetRarity && a.category == targetCategory
        );

        // 만약 해당 조건에 맞는 특성이 없다면 희귀도만 기준으로 다시 뽑기
        if (pool.Count == 0)
            pool = allAugments.FindAll(a => a.rarity == targetRarity);

        // 그래도 없다면 전체에서 뽑기
        if (pool.Count == 0)
            pool = allAugments;

        return pool[Random.Range(0, pool.Count)];
    }

    public void ShowAugmentOptions()
    {
        Time.timeScale = 0f;
        Weapon.IsPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        augmentPanel.SetActive(true);

        foreach (Transform child in optionParent)
            Destroy(child.gameObject);

        currentChoices.Clear();

        float spacingX = 300f;
        int totalCount = 3;
        float centerOffset = (totalCount - 1) * spacingX / 2f;

        for (int i = 0; i < totalCount; i++)
        {
            AugmentData picked = GetRandomAugment();
            currentChoices.Add(picked);

            GameObject buttonObj = Instantiate(augmentOptionPrefab, optionParent);
            RectTransform rect = buttonObj.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(i * spacingX - centerOffset, 0);

            buttonObj.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = picked.augmentName;
            buttonObj.transform.Find("Desc").GetComponent<TextMeshProUGUI>().text = picked.description;
            buttonObj.transform.Find("Icon").GetComponent<Image>().sprite = picked.icon;

            Image buttonImage = buttonObj.GetComponent<Image>();
            if (buttonImage != null)
                buttonImage.color = GetRarityColor(picked.rarity);

            buttonObj.GetComponent<Button>().onClick.AddListener(() => OnAugmentSelected(picked));
        }

        augmentPanel.SetActive(true);
    }

    private void OnAugmentSelected(AugmentData data)
    {
        augmentPanel.SetActive(false);
        AugmentSystem.Instance.ApplyAugment(data);
        Weapon.IsPaused = false;
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
