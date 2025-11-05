using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AugmentUIManager : MonoBehaviour
{
    public static AugmentUIManager Instance;

    [SerializeField] private GameObject augmentRoot;

    [Header("UI References")]
    public GameObject augmentPanel;
    public Transform optionParent;
    public GameObject augmentOptionPrefab;

    [Header("Augment Pool")]
    public List<AugmentData> allAugments = new List<AugmentData>();

    // 현재 보여주는 3개 선택지
    private readonly List<AugmentData> currentChoices = new List<AugmentData>();

    // 각 슬롯에 붙은 UI 버튼(재바인딩용)
    private readonly List<AugmentOptionButton> optionButtons = new List<AugmentOptionButton>();

    // 슬롯별 새로고침 1회 제한
    private bool[] rerollUsed;

    // 외부에서 확인용
    public bool IsOpen => augmentPanel != null && augmentPanel.activeSelf;

    private void Awake()
    {
        Instance = this;
        if (augmentPanel != null)
            augmentPanel.SetActive(false);
    }

    // 희귀도 색상 (기존 로직 유지)
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

    // 가중치 예시(기존 유지)
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

    // 단일 랜덤 뽑기(필터 우선 → 없으면 완화 → 전체)
    private AugmentData GetRandomAugment()
    {
        AugmentRarity targetRarity = GetRarityByChance();
        AugmentCategory targetCategory = GetCategoryByChance();

        List<AugmentData> pool = allAugments.FindAll(a => a.rarity == targetRarity && a.category == targetCategory);
        if (pool.Count == 0) pool = allAugments.FindAll(a => a.rarity == targetRarity);
        if (pool.Count == 0) pool = allAugments;

        return pool[Random.Range(0, pool.Count)];
    }

    // 현재 목록과 중복을 피하려고 시도하는 랜덤 (실패 시 그냥 허용)
    private AugmentData GetRandomAugmentDistinct(int avoidIndex = -1)
    {
        const int MaxTry = 20;
        for (int t = 0; t < MaxTry; t++)
        {
            var pick = GetRandomAugment();
            bool dup = false;
            for (int i = 0; i < currentChoices.Count; i++)
            {
                if (i == avoidIndex) continue;
                if (currentChoices[i] == pick) { dup = true; break; }
            }
            if (!dup) return pick;
        }
        // 20번 시도해도 겹치면 그냥 반환
        return GetRandomAugment();
    }

    // === 공개: 증강 선택 UI 열기 ===
    public void ShowAugmentOptions()
    {
        Time.timeScale = 0f;
        Weapon.IsPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (augmentPanel != null) augmentPanel.SetActive(true);

        // 기존 아이템 제거
        foreach (Transform child in optionParent) Destroy(child.gameObject);
        currentChoices.Clear();
        optionButtons.Clear();

        // 슬롯 수(3개) & 좌우 배치(기존 유지)
        int totalCount = 3;
        float spacingX = 300f;
        float centerOffset = (totalCount - 1) * spacingX / 2f;

        // 새로고침 플래그 초기화
        rerollUsed = new bool[totalCount];

        for (int i = 0; i < totalCount; i++)
        {
            var picked = GetRandomAugmentDistinct();
            currentChoices.Add(picked);

            // 프리팹 생성
            GameObject go = Instantiate(augmentOptionPrefab, optionParent);
            var rect = go.GetComponent<RectTransform>();
            if (rect != null) rect.anchoredPosition = new Vector2(i * spacingX - centerOffset, 0f);

            // UI 세팅 (이름/설명/아이콘/테두리컬러)
            if (go.transform.Find("Name")) go.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = picked.augmentName;
            if (go.transform.Find("Desc")) go.transform.Find("Desc").GetComponent<TextMeshProUGUI>().text = picked.description;
            if (go.transform.Find("Icon") && picked.icon) go.transform.Find("Icon").GetComponent<Image>().sprite = picked.icon;
            var img = go.GetComponent<Image>();
            if (img) img.color = GetRarityColor(picked.rarity);

            // 컴포넌트 참조
            var opt = go.GetComponent<AugmentOptionButton>();
            if (opt == null) opt = go.AddComponent<AugmentOptionButton>();
            optionButtons.Add(opt);

            int idx = i; // 캡처용

            // 선택/새로고침 콜백 바인딩
            opt.Bind(
                picked,
                onPick: () => OnAugmentSelected(currentChoices[idx]),
                onReroll: () => RefreshOne(idx),
                canReroll: !rerollUsed[idx] // 처음엔 항상 true
            );
        }

        // 장식 그래픽이 클릭 막지 않도록(원본 유지)
        UIRaycastUtil.MakeDecorationsNonBlocking(augmentPanel.transform);

        if (augmentPanel != null) augmentPanel.SetActive(true);
    }

    // === 슬롯 하나만 새로고침(1회 제한) ===
    private void RefreshOne(int index)
    {
        if (rerollUsed == null || index < 0 || index >= rerollUsed.Length) return;
        if (rerollUsed[index]) return; // 이미 사용

        // 새로운 증강 뽑기 (가능하면 중복 회피)
        var newPick = GetRandomAugmentDistinct(index);
        currentChoices[index] = newPick;
        rerollUsed[index] = true;

        // 슬롯 UI 업데이트(이름/설명/아이콘/컬러/버튼 콜백 재바인딩)
        var opt = optionButtons[index];
        if (opt != null)
        {
            // 배경 컬러 갱신
            var img = opt.GetComponent<Image>();
            if (img) img.color = GetRarityColor(newPick.rarity);

            // 텍스트/아이콘 & 콜백 갱신, 새로고침 버튼은 비활성화
            opt.Bind(
                newPick,
                onPick: () => OnAugmentSelected(newPick),
                onReroll: null,
                canReroll: false
            );
        }
    }

    // === 기존 선택 처리(유지) ===
    private void OnAugmentSelected(AugmentData data)
    {
        if (augmentPanel != null) augmentPanel.SetActive(false);

        AugmentSystem.Instance.ApplyAugment(data);
        Weapon.IsPaused = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log($"[AugmentUIManager] {data.augmentName} 선택됨");
    }

    // 교체 메뉴(추후 구현용 훅) — 기존 유지
    public void OpenReplaceMenu(AugmentData newAug, IReadOnlyList<AugmentData> equippedList)
    {
        Debug.Log($"[AugmentUIManager] 교체 메뉴 열기: {newAug.augmentName}");
    }

    // 열려있을 때 항상 UI모드 유지 — 기존 유지
    void LateUpdate()
    {
        if (IsOpen)
        {
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible) Cursor.visible = true;

            Time.timeScale = 0f;
            Weapon.IsPaused = true;
        }
    }
}
