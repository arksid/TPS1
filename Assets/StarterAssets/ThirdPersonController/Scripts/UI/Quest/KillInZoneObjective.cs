// Assets/StarterAssets/ThirdPersonController/Scripts/Quest/Objectives/KillInZoneObjective.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class KillInZoneObjective : QuestObjective
{
    [Header("섬멸 목표 수")]
    public int targetKills = 20;
    private int currentKills = 0;

    [Header("거점 구역(이 오브젝트의 Collider를 사용)")]
    private Collider zoneCollider;

    public override string ProgressText => $"{currentKills} / {targetKills}";
    public override bool IsCompleted => currentKills >= targetKills;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true; // 안전하게 트리거 권장
    }

    public override void Activate()
    {
        base.Activate();
        currentKills = 0;
        QuestEvents.OnEnemyDied += OnEnemyDied;
    }

    public override void Deactivate()
    {
        QuestEvents.OnEnemyDied -= OnEnemyDied;
        base.Deactivate();
    }

    private void OnEnemyDied(Vector3 deathPos, GameObject enemy)
    {
        if (zoneCollider == null) return;
        // 구역 내부 판정(3D) — 간단히 ClosestPoint 이용
        var closest = zoneCollider.ClosestPoint(deathPos);
        bool inside = Vector3.SqrMagnitude(closest - deathPos) < 0.001f;

        if (inside)
        {
            currentKills++;
            QuestManager.Instance?.OnObjectiveProgressPing(this);
        }
    }

    // Scene에서 영역 보이기
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
        var col = GetComponent<Collider>();
        if (col is BoxCollider b)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(b.center, b.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
