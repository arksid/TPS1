// Assets/StarterAssets/ThirdPersonController/Scripts/Quest/Quest.cs
using System.Collections.Generic;
using UnityEngine;

public class Quest : MonoBehaviour
{
    [Header("퀘스트 이름/설명")]
    public string questName = "새 퀘스트";
    [TextArea] public string description;

    [Header("목표들(순차 달성)")]
    public List<QuestObjective> objectives = new List<QuestObjective>();

    public int CurrentIndex { get; private set; } = 0;
    public bool IsCompleted => CurrentIndex >= objectives.Count;

    public QuestObjective CurrentObjective =>
        (CurrentIndex >= 0 && CurrentIndex < objectives.Count) ? objectives[CurrentIndex] : null;

    private void OnEnable()
    {
        // 시작 시 첫 목표 활성화
        if (!IsCompleted && CurrentObjective != null)
            CurrentObjective.Activate();
    }

    private void Update()
    {
        if (IsCompleted) return;
        if (CurrentObjective == null) return;

        if (CurrentObjective.IsCompleted)
        {
            CurrentObjective.Deactivate();
            CurrentIndex++;

            if (!IsCompleted && CurrentObjective != null)
                CurrentObjective.Activate();

            QuestManager.Instance?.OnQuestProgressChanged(this);
        }
    }
}
