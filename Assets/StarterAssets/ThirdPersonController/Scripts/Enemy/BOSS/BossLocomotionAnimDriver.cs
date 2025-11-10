using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
public class BossLocomotionAnimDriver : MonoBehaviour
{
    [Header("참조 (비워두면 자동 탐색)")]
    public Animator anim;
    public NavMeshAgent agent;
    public Rigidbody rb;

    [Header("애니메이터 파라미터 이름 (Float)")]
    public string moveXParam = "MoveX";
    public string moveZParam = "MoveZ";
    public string speedParam = "Speed";

    [Header("속도 1.0 기준(m/s)")]
    public float maxRefSpeed = 6f;

    [Header("스무딩")]
    public float dampTime = 0.08f;

    [Header("모델축 보정")]
    public bool invertX = false;
    public bool invertZ = false;

    // 내부
    Vector3 _prevPos;
    int _idMoveX, _idMoveZ, _idSpeed;
    bool _hasMoveX, _hasMoveZ, _hasSpeed;

    void Reset()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
    }

    void Awake()
    {
        if (!anim) anim = GetComponent<Animator>();
        _prevPos = transform.position;

        // 파라미터 해시
        _idMoveX = Animator.StringToHash(moveXParam);
        _idMoveZ = Animator.StringToHash(moveZParam);
        _idSpeed = Animator.StringToHash(speedParam);

        // 존재 여부 검사(없으면 경고만, 크래시 방지)
        _hasMoveX = HasParam(anim, moveXParam, AnimatorControllerParameterType.Float);
        _hasMoveZ = HasParam(anim, moveZParam, AnimatorControllerParameterType.Float);
        _hasSpeed = HasParam(anim, speedParam, AnimatorControllerParameterType.Float);

        if (!_hasMoveX) Debug.LogWarning($"[BossLocomotionAnimDriver] Animator에 Float '{moveXParam}' 없음");
        if (!_hasMoveZ) Debug.LogWarning($"[BossLocomotionAnimDriver] Animator에 Float '{moveZParam}' 없음");
        if (!_hasSpeed) Debug.LogWarning($"[BossLocomotionAnimDriver] Animator에 Float '{speedParam}' 없음");
    }

    void Update()
    {
        Vector3 v = GetWorldVelocity(); v.y = 0f;
        Vector3 lv = transform.InverseTransformDirection(v);

        float vx = invertX ? -lv.x : lv.x;
        float vz = invertZ ? -lv.z : lv.z;
        float speed = Mathf.Clamp01(v.magnitude / Mathf.Max(0.01f, maxRefSpeed));

        if (_hasMoveX) anim.SetFloat(_idMoveX, vx, dampTime, Time.deltaTime);
        if (_hasMoveZ) anim.SetFloat(_idMoveZ, vz, dampTime, Time.deltaTime);
        if (_hasSpeed) anim.SetFloat(_idSpeed, speed, dampTime, Time.deltaTime);
    }

    Vector3 GetWorldVelocity()
    {
        if (agent && agent.enabled) return agent.velocity;
        if (rb && rb.gameObject.activeInHierarchy && !rb.isKinematic) return rb.velocity;
        Vector3 cur = transform.position;
        Vector3 vel = (cur - _prevPos) / Mathf.Max(Time.deltaTime, 0.0001f);
        _prevPos = cur;
        return vel;
    }

    bool HasParam(Animator a, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in a.parameters)
            if (p.type == type && p.name == name) return true;
        return false;
    }
}
