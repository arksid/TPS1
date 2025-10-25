using System.Collections.Generic;
using UnityEngine;

public class AugmentUnlockManager : MonoBehaviour
{
    public static AugmentUnlockManager Instance;

    [Tooltip("증강 선택 UI가 열리는 레벨")]
    public List<int> unlockLevels = new List<int> { 1, 5, 10, 15, 20, 25, 30, 35 };

    private HashSet<int> alreadyTriggered = new HashSet<int>();

    private void Awake()
    {
        Instance = this;
    }

    public void OnPlayerLevelUp(int newLevel)
    {
        if (unlockLevels.Contains(newLevel) && !alreadyTriggered.Contains(newLevel))
        {
            alreadyTriggered.Add(newLevel);
            AugmentUIManager.Instance.ShowAugmentOptions();
        }
    }
}
