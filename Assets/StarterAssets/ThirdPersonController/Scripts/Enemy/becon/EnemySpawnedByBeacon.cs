using UnityEngine;

/// <summary>
/// 비콘이 소환한 적에 자동으로 붙는 컴포넌트.
/// 적이 파괴될 때 비콘에게 알려서 aliveCount를 줄여준다.
/// </summary>
public class EnemySpawnedByBeacon : MonoBehaviour
{
    [HideInInspector] public EnemyBeacon owner;

    void OnDestroy()
    {
        if (owner != null)
        {
            owner.NotifyChildDied();
        }
    }
}
