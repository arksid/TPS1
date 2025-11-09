using UnityEngine;
using UnityEngine.AI;

public class BossFacePlayer : MonoBehaviour
{
    public Transform target;          // 비워두면 Player 태그 자동 탐색
    public float rotateSpeed = 8f;
    public bool yawOnly = true;       // 수평(Yaw)만 회전
    public bool keepUpright = true;   // Pitch/Roll 강제 0

    NavMeshAgent agent;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void LateUpdate()
    {
        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }
        if (!target) return;

        // 목표 방향 계산
        Vector3 dir = target.position - transform.position;
        if (yawOnly) dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        // 회전 보간(Up은 항상 세계 Up)
        var wanted = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, wanted, rotateSpeed * Time.deltaTime);

        // Agent가 회전까지 건드리면 충돌 → Agent 회전 꺼두는 게 안전
        if (agent) agent.updateRotation = false;

        // 혹시라도 기울어졌다면 강제로 똑바로
        if (keepUpright)
        {
            var e = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, e.y, 0f);
        }
    }
}
