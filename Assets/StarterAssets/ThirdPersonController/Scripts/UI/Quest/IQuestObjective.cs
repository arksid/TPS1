// Assets/StarterAssets/ThirdPersonController/Scripts/Quest/IQuestObjective.cs
public interface IQuestObjective
{
    string Title { get; }
    string ProgressText { get; }
    bool IsCompleted { get; }
    void Activate();
    void Deactivate();
}
