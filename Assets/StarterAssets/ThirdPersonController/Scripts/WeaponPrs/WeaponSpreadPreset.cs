using UnityEngine;

[CreateAssetMenu(menuName = "FPS/Weapon Spread Preset", fileName = "NewWeaponSpreadPreset")]
public class WeaponSpreadPreset : ScriptableObject
{
    [Header("Base Spread (degrees)")]
    [Tooltip("비조준 기본 퍼짐")]
    public float hipFireSpread = 3f;
    [Tooltip("조준(Aim) 시 퍼짐")]
    public float aimSpread = 1f;
    [Tooltip("이동량(0~1)에 비례해서 추가되는 퍼짐")]
    public float moveSpread = 2f;
    [Tooltip("스프린트 시 추가 퍼짐")]
    public float sprintSpread = 6f;

    [Header("Bloom")]
    [Tooltip("발사할 때마다 누적되는 추가 퍼짐")]
    public float bloomPerShot = 0.3f;
    [Tooltip("초당 감소하는 블룸 값")]
    public float bloomDecayPerSec = 2f;
    [Tooltip("블룸 상한")]
    public float maxBloom = 5f;
}

