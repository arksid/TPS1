using UnityEngine;

public class BossVisualAlign : MonoBehaviour
{
    public Transform visual;          // 메쉬(자식) Transform
    public float visualYOffset = 0.4f; // 발 위치 보정(필요값 맞추세요)
    public bool forceUpright = true;   // 시각은 항상 수직

    void LateUpdate()
    {
        if (!visual) return;

        // 높이 보정
        var lp = visual.localPosition;
        lp.y = visualYOffset;
        visual.localPosition = lp;

        // 기울기 제거(머리 박는 현상 차단)
        if (forceUpright)
        {
            var rot = visual.rotation;
            visual.rotation = Quaternion.Euler(0f, rot.eulerAngles.y, 0f);
        }
    }
}
