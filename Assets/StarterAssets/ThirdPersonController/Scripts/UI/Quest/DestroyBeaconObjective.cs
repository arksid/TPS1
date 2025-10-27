// Assets/StarterAssets/ThirdPersonController/Scripts/Quest/Objectives/DestroyBeaconObjective.cs
using UnityEngine;

public class DestroyBeaconObjective : QuestObjective
{
    [Header("필요 개수")]
    public int targetCount = 5;
    private int currentCount = 0;

    public override string ProgressText => $"{currentCount} / {targetCount}";
    public override bool IsCompleted => currentCount >= targetCount;

    public override void Activate()
    {
        base.Activate();
        currentCount = 0;
        QuestEvents.OnBeaconDestroyed += OnBeaconDestroyed;
    }

    public override void Deactivate()
    {
        QuestEvents.OnBeaconDestroyed -= OnBeaconDestroyed;
        base.Deactivate();
    }

    private void OnBeaconDestroyed(GameObject beacon)
    {
        currentCount++;
        QuestManager.Instance?.OnObjectiveProgressPing(this);
    }
}
