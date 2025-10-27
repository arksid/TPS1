// Assets/StarterAssets/ThirdPersonController/Scripts/Quest/QuestEvents.cs
using System;
using UnityEngine;

public static class QuestEvents
{
    /// <summary>적이 사망했을 때: (월드좌표, 누구였는지)</summary>
    public static event Action<Vector3, GameObject> OnEnemyDied;

    /// <summary>비콘이 파괴되었을 때: (비콘 게임오브젝트)</summary>
    public static event Action<GameObject> OnBeaconDestroyed;

    public static void EnemyDied(Vector3 position, GameObject enemyGO)
        => OnEnemyDied?.Invoke(position, enemyGO);

    public static void BeaconDestroyed(GameObject beaconGO)
        => OnBeaconDestroyed?.Invoke(beaconGO);
}
