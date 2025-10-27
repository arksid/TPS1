// EnemyEvents.cs
using System;
using UnityEngine;

public static class EnemyEvents
{
    public static event Action<Vector3> OnEnemyKilled; // 사망 위치(필터링용)

    public static void RaiseEnemyKilled(Vector3 pos)
    {
        OnEnemyKilled?.Invoke(pos);
    }
}