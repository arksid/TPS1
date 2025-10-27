// Assets/StarterAssets/ThirdPersonController/Scripts/Quest/QuestObjective.cs
using UnityEngine;

public abstract class QuestObjective : MonoBehaviour, IQuestObjective
{
    [TextArea] public string title = "Objective";
    public string Title => title;

    public abstract string ProgressText { get; }
    public abstract bool IsCompleted { get; }

    // 필요 시 이벤트 구독 시작
    public virtual void Activate() { enabled = true; }
    // 필요 시 이벤트 구독 해제
    public virtual void Deactivate() { enabled = false; }
}
