using UnityEngine;

// 애니메이션/다른 스크립트보다 "맨 마지막"에 자세를 덮어씁니다.
[DefaultExecutionOrder(1000)]
public class BossUprightLock : MonoBehaviour
{
    [Header("시각(메쉬) 자식")]
    public Transform visual;                 // 보스 모델이 들어있는 자식(Visual)
                                             // 없으면 루트의 첫 자식을 자동 사용

    [Header("루트 회전 (수평만)")]
    public bool forceRootYawOnly = true;     // 루트는 Yaw만 허용(피치/롤=0)
    public float rootYawLerp = 12f;          // 플레이어 쪽으로 부드럽게 회전
    public Transform player;                 // 비우면 태그로 자동 탐색
    public string playerTag = "Player";

    [Header("메쉬 축 잠금 (눕기 방지 핵심)")]
    public bool lockVisualPitchRoll = true;  // ON이면 피치/롤을 우리가 강제
    [Tooltip("메쉬의 X축(피치) 각도. 대개 0, +90, -90 중 하나를 선택")]
    public float visualPitchDeg = 0f;        // 예: -90 또는 +90
    [Tooltip("메쉬의 Z축(롤) 각도. 누웠다면 +90/-90로 바꿔보세요")]
    public float visualRollDeg = 0f;         // 예: 0 → 눕는다면 ±90
    [Tooltip("메쉬의 Y축(야우) 보정. 보스가 옆/뒤를 보면 90/180으로")]
    public bool overrideVisualLocalYaw = false;
    public float visualYawDeg = 0f;          // 필요 시 0/90/180/270

    [Header("시각 위치 보정(옵션)")]
    public float visualYOffset = 0f;         // 메쉬만 살짝 띄우기(관통 방지)

    // 내부
    bool _autoFoundVisual;

    void Awake()
    {
        if (!visual && transform.childCount > 0)
        {
            visual = transform.GetChild(0);
            _autoFoundVisual = true;
        }
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p) player = p.transform;
        }
    }

    void LateUpdate()
    {
        // 1) 루트는 수평만 유지
        if (forceRootYawOnly)
        {
            float yaw = transform.eulerAngles.y;

            // 플레이어가 있으면 그쪽을 보게(수평만)
            if (player)
            {
                Vector3 dir = player.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    float targetYaw = Quaternion.LookRotation(dir, Vector3.up).eulerAngles.y;
                    yaw = Mathf.LerpAngle(yaw, targetYaw, 1f - Mathf.Exp(-rootYawLerp * Time.deltaTime));
                }
            }

            // 피치/롤=0, 야우만 유지
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        // 2) 시각(메쉬) 축/위치 잠금(눕기 방지의 핵심)
        if (visual)
        {
            // 위치 보정
            if (Mathf.Abs(visualYOffset) > 0.0001f)
            {
                var lp = visual.localPosition;
                lp.y = visualYOffset;
                visual.localPosition = lp;
            }

            // 로컬 오일러를 가져와서 우리가 고정할 축만 덮어씀
            var le = visual.localEulerAngles;

            if (lockVisualPitchRoll)
            {
                le.x = visualPitchDeg;  // 피치 강제
                le.z = visualRollDeg;   // 롤 강제
            }

            if (overrideVisualLocalYaw)
            {
                le.y = visualYawDeg;    // 필요 시 야우 보정
            }

            visual.localEulerAngles = le;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // 각도 범위 보기 좋게 정리
        visualPitchDeg = Normalize360(visualPitchDeg);
        visualRollDeg = Normalize360(visualRollDeg);
        visualYawDeg = Normalize360(visualYawDeg);
    }
    float Normalize360(float a)
    {
        a %= 360f; if (a < 0f) a += 360f; return a;
    }
#endif
}
