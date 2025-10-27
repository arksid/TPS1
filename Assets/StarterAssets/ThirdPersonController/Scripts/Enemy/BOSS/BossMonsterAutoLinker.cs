// Assets/Scripts/Boss/BossMonsterAutoLinker.cs
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(BossMonster))]
public class BossMonsterAutoLinker : MonoBehaviour
{
    [Tooltip("자식에서 DamageablePart를 찾아 boss 필드를 자동 연결합니다.")]
    public bool autoAssignBossOnParts = true;

#if UNITY_EDITOR
    [ContextMenu("Auto Link Parts (Assign DamageablePart.boss)")]
    public void AutoLink()
    {
        var boss = GetComponent<BossMonster>();
        if (boss == null)
        {
            Debug.LogError("[AutoLink] BossMonster가 없습니다.");
            return;
        }

        var parts = GetComponentsInChildren<DamageablePart>(includeInactive: true);
        int linked = 0;

        foreach (var p in parts)
        {
            if (p == null) continue;
            if (autoAssignBossOnParts && p.boss == null)
            {
                Undo.RecordObject(p, "Assign Boss on Part");
                p.boss = boss;
                EditorUtility.SetDirty(p);
                linked++;
            }

            // Hitbox.part 자동 연결(누락 대비)
            var hitboxes = p.GetComponentsInChildren<Hitbox>(includeInactive: true);
            foreach (var hb in hitboxes)
            {
                if (hb != null && hb.part == null)
                {
                    Undo.RecordObject(hb, "Assign Part on Hitbox");
                    hb.part = p;
                    EditorUtility.SetDirty(hb);
                }
            }
        }

        Debug.Log($"[AutoLink] DamageablePart {parts.Length}개 검색, boss 자동 연결 {linked}개 완료");
    }
#endif
}
