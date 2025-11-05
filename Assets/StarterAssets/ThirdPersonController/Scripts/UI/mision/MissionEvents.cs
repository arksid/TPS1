using System;

public static class MissionEvents
{
    public static event Action OnEnemyKilled;
    public static void RaiseEnemyKilled() => OnEnemyKilled?.Invoke();
}
