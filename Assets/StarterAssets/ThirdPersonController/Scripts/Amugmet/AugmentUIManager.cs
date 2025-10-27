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
    // 클래스 안 어딘가에 추가
    public bool IsOpen => augmentPanel != null && augmentPanel.activeSelf;

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
        if (roll < 0.6f) return AugmentRarity.Normal;
        else if (roll < 0.85f) return AugmentRarity.Rare;
        else if (roll < 0.95f) return AugmentRarity.Epic;
        else return AugmentRarity.Legendary;
    }

    private AugmentCategory GetCategoryByChance()
    {
        float roll = Random.value;
        if (roll < 0.8f) return AugmentCategory.Normal;
        else return AugmentCategory.Special;
    }

    private AugmentData GetRandomAugment()
    {
        AugmentRarity targetRarity = GetRarityByChance();
        AugmentCategory targetCategory = GetCategoryByChance();

        List<AugmentData> pool = allAugments.FindAll(
            a => a.rarity == targetRarity && a.category == targetCategory
        );

        if (pool.Count == 0)
            pool = allAugments.FindAll(a => a.rarity == targetRarity);

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

    // ✅ 추가됨 : 교체 메뉴 열기용
    public void OpenReplaceMenu(AugmentData newAug, IReadOnlyList<AugmentData> equippedList)
    {
        Debug.Log($"[AugmentUIManager] 교체 메뉴 열기: {newAug.augmentName}");
        // 나중에 실제 UI를 만들어서 선택하게 할 수 있음
    }

    // ✅ 추가됨 : 선택 이벤트 처리용
    public void OnPickAugment(AugmentData data)
    {
        augmentPanel.SetActive(false);
        AugmentSystem.Instance.ApplyAugment(data);
        Weapon.IsPaused = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log($"[AugmentUIManager] {data.augmentName} 선택됨");
    }
    // AugmentUIManager 안에 추가
    void LateUpdate()
    {
        if (IsOpen)
        {
            // 증강 UI가 열려 있으면 항상 'UI 모드' 강제
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible) Cursor.visible = true;

            Time.timeScale = 0f;   // 혹시 다른 곳에서 건드려도 유지
            Weapon.IsPaused = true;
        }
    }

}
