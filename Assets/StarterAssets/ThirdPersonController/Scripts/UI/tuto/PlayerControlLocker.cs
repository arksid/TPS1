using UnityEngine;
using StarterAssets;

[DisallowMultipleComponent]
public class PlayerControlLocker : MonoBehaviour
{
    [Header("자동 할당 (비워두면 GetComponent)")]
    public ThirdPersonController tpc;
    public CharacterController cc;
    public Character character;

    void Awake()
    {
        if (!tpc) tpc = GetComponent<ThirdPersonController>();
        if (!cc) cc = GetComponent<CharacterController>();
        if (!character) character = GetComponent<Character>();
    }

    public void LockControls(bool on)
    {
        if (tpc) tpc.enabled = !on;          // 이동/사격 등 컨트롤 비활성
        if (cc) cc.enabled = !on;          // 물리 이동 비활성
        // 필요하면 무기 쏘기 중단 등 추가
        if (on)
        {
            character?.weapon?.StopFiring();
        }
    }
}
