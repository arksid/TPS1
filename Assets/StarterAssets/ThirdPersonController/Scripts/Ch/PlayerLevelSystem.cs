using UnityEngine;
using System;

public class PlayerLevelSystem : MonoBehaviour
{
    public static PlayerLevelSystem Instance;

    [Header("경험치 설정")]
    public int currentLevel = 1;
    public int currentExp = 0;
    public int expToNextLevel = 100; // 레벨업에 필요한 경험치
    public float levelExpMultiplier = 1.5f; // 다음 레벨로 갈수록 필요 경험치 증가

    public event Action<int> OnLevelUp;
    public event Action<int, int> OnExpChanged;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        OnExpChanged += CanvasManager.singleton.UpdateExpUI;

        // 게임 시작 시 초기 경험치 표시
        CanvasManager.singleton.UpdateExpUI(currentExp, expToNextLevel);
    }


    public void AddExp(int amount)
    {
        currentExp += amount;
        OnExpChanged?.Invoke(currentExp, expToNextLevel);

        if (currentExp >= expToNextLevel)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentExp -= expToNextLevel;
        currentLevel++;
        expToNextLevel = Mathf.RoundToInt(expToNextLevel * levelExpMultiplier);

        OnLevelUp?.Invoke(currentLevel);
        OnExpChanged?.Invoke(currentExp, expToNextLevel);

        // 🪄 레벨업 시 증강 UI 열기
        AugmentUIManager.Instance.ShowAugmentOptions();
    }

}
