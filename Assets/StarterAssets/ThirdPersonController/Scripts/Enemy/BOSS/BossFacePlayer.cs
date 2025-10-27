using UnityEngine;

public class BossFacePlayer : MonoBehaviour
{
    [Tooltip("직접 타깃을 지정하지 않으면 Character.Instance를 자동 사용")]
    public Transform target;

    [Header("회전 세팅")]
    public float turnSpeedDegPerSec = 360f;  // 1초당 회전 속도(도)
    public bool lockPitch = true;            // Yaw만 돌리고 싶으면 켜두기 (상/하 고개 금지)

    void LateUpdate()
    {
        Transform t = target;
        if (t == null && Character.Instance != null) t = Character.Instance.transform;
        if (t == null) return;

        Vector3 to = t.position - transform.position;
        if (lockPitch) to.y = 0f; // 상하 고개 고정(선택)

        if (to.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(to.normalized, Vector3.up);
        float maxStep = turnSpeedDegPerSec * Time.deltaTime;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, maxStep);
    }
}
