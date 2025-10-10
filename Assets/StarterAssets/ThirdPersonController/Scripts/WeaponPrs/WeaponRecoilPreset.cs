using UnityEngine;

[CreateAssetMenu(menuName = "Weapon/Recoil Preset")]
public class WeaponRecoilPreset : ScriptableObject
{
    [Header("Recoil Settings")]
    public float verticalRecoil = 0.1f;
    public float horizontalRecoil = 0.05f;

    [Header("Recoil Recovery Speed")]
    public float recoveryX = 8f; // 좌우 복원 속도
    public float recoveryY = 6f; // 수직 복원 속도
}
