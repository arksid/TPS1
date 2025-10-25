using System.Collections.Generic;
using UnityEngine;

public class AugmentSlotManager : MonoBehaviour
{
    public static AugmentSlotManager Instance;

    [SerializeField] private int maxSlots = 8;
    private readonly List<AugmentData> equipped = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public IReadOnlyList<AugmentData> GetEquipped() => equipped;

    public void AddOrReplace(AugmentData newAug)
    {
        if (newAug == null) return;

        if (equipped.Count < maxSlots)
        {
            equipped.Add(newAug);
            AugmentSystem.Instance.ApplyAugment(newAug);
        }
        else
        {
            // 꽉 찼으면 교체 메뉴 오픈
            AugmentUIManager.Instance.OpenReplaceMenu(newAug, equipped);
        }
    }

    public void ReplaceAt(int index, AugmentData newAug)
    {
        if (index < 0 || index >= equipped.Count || newAug == null) return;

        // 기존 제거 → 신규 적용
        AugmentSystem.Instance.RemoveAugment(equipped[index]);
        equipped[index] = newAug;
        AugmentSystem.Instance.ApplyAugment(newAug);
    }
}
